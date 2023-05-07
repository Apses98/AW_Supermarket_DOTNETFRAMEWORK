using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace AW_Supermarket_DOTNETFRAMEWORK
{
    public partial class mainForm : Form
    {
        /* Declarations
         *  autoSync:       New Thread to run the Auto Sync on
         *  Controller:     Manages majority of checks before comunication with the backend
         *  lastReceipt:    is a string that contains all the text of the last sold or returned products.
         *  historyChart:   is the chart used to plot the price and stock history
         */
        Thread autoSync;
        Controller controller;
        string lastReceipt;
        Chart historyChart;
        public mainForm()
        {
            InitializeComponent();
            controller = new Controller(this);
        }

        /* Events */
        private void mainForm_Load(object sender, EventArgs e)
        {
            
            productTypeComboBox.SelectedIndex = 0;
            lastReceipt = string.Empty;
            historyChart = new Chart();
            historyChart.Dock = DockStyle.Fill;
            updateDataGridView();

        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            /* Prints the text in the lastReceipt variable */
            Graphics g = e.Graphics;
            Font font = new Font("Arial", 20);
            SolidBrush brush = new SolidBrush(Color.Black);
            g.DrawString(lastReceipt, font, brush, 0, 0);
        }

        private void productTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            /* Updates the status of the textboxes (enabled/disabled) */
            updateTextboxesEnabledStatus(productTypeComboBox.SelectedIndex);
        }

        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            /* Aborts the autoSync Thread (if it is still running) on form cloasing and saves data to database*/
            if (autoSync != null)
            {
                autoSync.Abort();
            }
            controller.FormColsing();
        }

        private void addToCartButton_Click(object sender, EventArgs e)
        {
            /* Updates the cart */
            updateCart();
        }

        private async void sellButton_Click(object sender, EventArgs e)
        {
            /* Checks the quantity of each item in the cart before selling */

            foreach (var item in cartListBox.Items)
            {
                try
                {
                    if (controller.getQuantity(item) == 0)
                    {
                        MessageBox.Show($"Product {item.ToString().Split('\t')[1]} is out of stock!");
                        return;
                    }
                    else if (int.Parse(item.ToString().Split('\t')[2].Split('x')[1]) > controller.getQuantity(item))
                    {
                        MessageBox.Show($"Error!, There is only {controller.getQuantity(item)} piece of {item.ToString().Split('\t')[1]} in the inventory!\nYou can not sell more than what you have in the inventory!");
                        return;
                    }
                }
                catch (Exception)
                {
                    return;
                }

            }
            // Sync, Sell, send the update to the api then print receipt
            syncNowButton_Click(sender, e);
            await Task.Run(() =>
            {
                controller.sell_returnButtonPressed(cartListBox, "sell");
                controller.updateQuantityInCentral(cartListBox, apiTextBox.Text);
            });
            lastReceipt = " ";
            printReceipt(cartListBox);
            cartListBox.Items.Clear();
            updateDataGridView();
            TotalLabel.Text = "Total: 0";
        }

        private void removeFromCartButton_Click(object sender, EventArgs e)
        {
            // Removes a selected item from the cart and updates the total price
            cartListBox.Items.Remove(cartListBox.SelectedItem);
            updateTotalPrice();
        }

        private void addProductButton_Click(object sender, EventArgs e)
        {
            /* Adds new product and clears the textboxes afterwards */
            if (!(productTypeComboBox.Text == "Book" || productTypeComboBox.Text == "Film" || productTypeComboBox.Text == "Game"))
            {
                MessageBox.Show("Please select correct product type!");
                return;
            }
            if (controller.addProductButtonPressed(
                productIDtextBox.Text,
                nametextBox.Text,
                pricetextBox.Text,
                authortextBox.Text,
                genretextBox.Text,
                formattextBox.Text,
                languagetextBox.Text,
                platformtextBox.Text,
                playtimetextBox.Text,
                productTypeComboBox.SelectedItem.ToString(),
                quantityTextBox.Text
                ))
            {
                clearTextBoxes();
            }


        }

        private void deleteProductButton_Click(object sender, EventArgs e)
        {
            /* Deletes a product from the product list 
             * Checks if the quantity is not zero and asks the user if they really want to delete */
            bool quantityNotZero = false;
            DialogResult result = DialogResult.Yes;
            if (dataGridView2.SelectedRows.Count > 0)
            {
                for (int i = 0; i < dataGridView2.SelectedRows.Count; i++)
                {
                    try
                    {
                        if (int.Parse(dataGridView2.SelectedRows[i].Cells[1].Value.ToString()) > 0)
                        {
                            quantityNotZero = true;
                        }
                    }
                    catch (Exception)
                    {
                        return;
                    }

                }
                if (quantityNotZero)
                {
                    result = MessageBox.Show(
                        "You are trying to delete products that have quantity more than zero in the inventory\nDelete anyway?",
                        "Delete product(s)",
                        MessageBoxButtons.YesNo);
                }
                if (result != DialogResult.Yes)
                    return;
                controller.deleteProductButtonPressed(dataGridView2.SelectedRows);
            }
            else
            {
                MessageBox.Show("Please select a product to delete!");
            }


        }

        private void addToOrderButton_Click(object sender, EventArgs e)
        {
            // updates the order listbox
            updateNewOrderList();
        }

        private void RemoveFromOrderButton_Click(object sender, EventArgs e)
        {
            // Removes the selected item from the orderListBox
            if (orderListBox.SelectedIndex == -1)
                return;
            orderListBox.Items.RemoveAt(orderListBox.SelectedIndex);
        }

        private async void orderNowButton_Click(object sender, EventArgs e)
        {
            /* Syncs with the api to update the current price and stock, then preforms the order. 
               Afterwards, it updates the new quantity in the central stock(api)*/
            if (orderListBox.Items.Count == 0)
                return;
            syncNowButton_Click(sender, e);
            
            await Task.Run(() =>
            {
                controller.orderNowButtonPressed(orderListBox);
                controller.updateQuantityInCentral(orderListBox, apiTextBox.Text);
            });
            orderListBox.Items.Clear();
            updateDataGridView();
        }

        private void returnButton_Click(object sender, EventArgs e)
        {
            /* Sync befor returning, returns, then updates the quantity/stock in central(api).
             * Print receipt */
            if (cartListBox.Items.Count == 0)
                return;
            syncNowButton_Click(sender, e);
            controller.sell_returnButtonPressed(cartListBox, "return");
            controller.updateQuantityInCentral(cartListBox, apiTextBox.Text);
            lastReceipt = " ";
            printReceipt(cartListBox);
            cartListBox.Items.Clear();
            updateDataGridView();
            TotalLabel.Text = "Total: 0";
        }

        private void searchTextBox_TextChanged(object sender, EventArgs e)
        {
            //Search and refresh the data grid view
            if (searchTextBox.Text == "")
            {
                updateDataGridView();
            }
            else
            {
                dataGridView2.DataSource = controller.searchFor(searchTextBox.Text);
                dataGridView2.Refresh();
            }
        }

        private void toptenButton_Click(object sender, EventArgs e)
        {
            // Displays the top ten sold products
            MessageBox.Show(controller.top10AndTotalSales(yearRadioButton.Checked, true));
        }

        private void reprintButton_Click(object sender, EventArgs e)
        {
            // Reprints the last receipt
            printReceipt(cartListBox);
        }

        private void totalSalesButton_Click(object sender, EventArgs e)
        {
            // Displays the best sold products for the entier year
            MessageBox.Show(controller.top10AndTotalSales(yearRadioButton.Checked, false));
        }

        private async void syncNowButton_Click(object sender, EventArgs e)
        {
            // Syncs with the api, then updates the dataGridView
            apiTextBox.Enabled = false;
            syncNowButton.Enabled = false;
            syncNowButton.BackColor = Color.Yellow;
            syncNowButton.Text = "Running...";
            if (controller.syncNowButtonPressed(apiTextBox.Text))
            {

                syncNowButton.BackColor = Color.LightGreen;
                updateDataGridView();
                syncNowButton.Text = "Success!";
                await Task.Run(() =>
                {
                    Thread.Sleep(1500);
                });

            }
            else
            {
                syncNowButton.BackColor = Color.LightCoral;
                syncNowButton.Text = "Unable to sync!!";
                await Task.Run(() =>
                {
                    Thread.Sleep(1500);
                });
            }
            apiTextBox.Enabled = true;
            syncNowButton.Enabled = true;
            syncNowButton.Text = "Sync Now";
            syncNowButton.BackColor = Color.Transparent;
        }

        private async void autoSyncButton_Click(object sender, EventArgs e)
        {
            /* Creates a new Thread (autoSync) which syncs with the api every 1 minute */
            if (!controller.apiIsValid(apiTextBox.Text))
            {
                syncNowButton.BackColor = Color.LightCoral;
                syncNowButton.Text = "Error, Check your API";
                await Task.Run(() =>
                {
                    Thread.Sleep(1500);
                });
                syncNowButton.Text = "Sync Now";
                syncNowButton.BackColor = Color.Transparent;
                return;
            }
            autoSync = new Thread(() => controller.autoSyncButtonPressed(apiTextBox.Text));
            if (autoSyncButton.Text == "Auto Sync: Off")
            {
                
                autoSyncButton.Text = "Auto Sync: On";
                autoSyncButton.BackColor = Color.LightGreen;
                apiTextBox.Enabled = false;
                syncNowButton.Enabled = false;
                controller.autoSyncThreadIsRunning = true;
                autoSync.Start();
            }
            else
            {
                controller.autoSyncThreadIsRunning = false;
                autoSync.Abort();
                autoSyncButton.Text = "Auto Sync: Off";
                autoSyncButton.BackColor = Color.LightCoral;
                apiTextBox.Enabled = true;
                syncNowButton.Enabled = true;
                syncNowButton.Text = "Sync Now";
                syncNowButton.BackColor = Color.Transparent;
            }
        }

        private void plotNowButton_Click(object sender, EventArgs e)
        {
            // Plot the chart/Values
            drawChart();
        }


        /* Other functions */
        private void updateTextboxesEnabledStatus(int selectedIndex)
        {
            /* Updates the Enabled Status of the textboxes in the inventory tab */
            if (selectedIndex == 0)
            {
                productIDtextBox.Enabled = true;
                nametextBox.Enabled = true;
                pricetextBox.Enabled = true;
                authortextBox.Enabled = true;
                genretextBox.Enabled = true;
                formattextBox.Enabled = true;
                languagetextBox.Enabled = true;
                platformtextBox.Enabled = false;
                playtimetextBox.Enabled = false;
                quantityTextBox.Enabled = true;
                platformtextBox.Text = "";
                playtimetextBox.Text = "";
                quantityTextBox.Text = "";
            }
            else if (selectedIndex == 1)
            {
                productIDtextBox.Enabled = true;
                nametextBox.Enabled = true;
                pricetextBox.Enabled = true;
                authortextBox.Enabled = false;
                genretextBox.Enabled = false;
                authortextBox.Text = "";
                genretextBox.Text = "";
                formattextBox.Enabled = true;
                languagetextBox.Enabled = false;
                platformtextBox.Enabled = false;
                languagetextBox.Text = "";
                platformtextBox.Text = "";
                playtimetextBox.Enabled = true;
                quantityTextBox.Enabled = true;
                quantityTextBox.Text = "";
            }
            else if (selectedIndex == 2)
            {
                productIDtextBox.Enabled = true;
                nametextBox.Enabled = true;
                pricetextBox.Enabled = true;
                authortextBox.Enabled = false;
                genretextBox.Enabled = false;
                formattextBox.Enabled = false;
                languagetextBox.Enabled = false;
                authortextBox.Text = "";
                genretextBox.Text = "";
                formattextBox.Text = "";
                languagetextBox.Text = "";
                platformtextBox.Enabled = true;
                playtimetextBox.Enabled = false;
                playtimetextBox.Text = "";
                quantityTextBox.Enabled = true;
                quantityTextBox.Text = "";
            }
        }

        private void updateDataGridView()
        {
            // Updates the data gridViews
            dataGridView1.DataSource = controller.getDataSource();
            dataGridView2.DataSource = controller.getDataSource();
            dataGridView3.DataSource = controller.getDataSource();
            dataGridView1.Refresh();
            dataGridView2.Refresh();
            dataGridView3.Refresh();
        }

        private void clearTextBoxes()
        {
            // Clears the text in the textboxes
            productIDtextBox.Text = string.Empty;
            pricetextBox.Text = string.Empty;
            authortextBox.Text = string.Empty;
            nametextBox.Text = string.Empty;
            genretextBox.Text = string.Empty;
            formattextBox.Text = string.Empty;
            languagetextBox.Text = string.Empty;
            platformtextBox.Text = string.Empty;
            playtimetextBox.Text = string.Empty;
            quantityTextBox.Text = string.Empty;

        }

        private void updateCart()
        {
            // Updates the cartListBox
            int x = 1;
            if (cartListBox.Items.Count == 0)
            {
                cartListBox.Items.Add(dataGridView1.SelectedRows[0].Cells[0].Value.ToString() + '\t' + dataGridView1.SelectedRows[0].Cells[3].Value.ToString() + "\t" + dataGridView1.SelectedRows[0].Cells[4].Value + " x " + '1');
                updateTotalPrice();
                return;
            }
            for (int i = 0; i < cartListBox.Items.Count; i++)
            {
                if (cartListBox.Items[i].ToString().Split('\t')[1] == dataGridView1.SelectedRows[0].Cells[3].Value.ToString())
                {
                    try
                    {
                        x = int.Parse(cartListBox.Items[i].ToString().Split('\t')[2].Split('x')[1].ToString()) + 1;
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    cartListBox.Items.Add(dataGridView1.SelectedRows[0].Cells[0].Value.ToString() + '\t' + dataGridView1.SelectedRows[0].Cells[3].Value + "\t" + dataGridView1.SelectedRows[0].Cells[4].Value + " x " + x.ToString());
                    cartListBox.Items.RemoveAt(i);
                }
                else
                {
                    x = 1;
                }
            }
            if (x == 1)
            {
                cartListBox.Items.Add(dataGridView1.SelectedRows[0].Cells[0].Value.ToString() + '\t' + dataGridView1.SelectedRows[0].Cells[3].Value + "\t" + dataGridView1.SelectedRows[0].Cells[4].Value + " x " + x.ToString());
            }

            updateTotalPrice();
        }

        private void updateNewOrderList()
        {
            // Updates the orderListBox
            int x = 1, noMatch = 0;
            if (orderListBox.Items.Count == 0)
            {
                for (int i = 0; i < dataGridView2.SelectedRows.Count; i++)
                {
                    orderListBox.Items.Add(dataGridView2.SelectedRows[i].Cells[0].Value.ToString() + '\t' + dataGridView2.SelectedRows[i].Cells[3].Value.ToString() + "\t" + " x " + '1');
                }
                return;
            }
            for (int i = 0; i < dataGridView2.SelectedRows.Count; i++)
            {

                for (int j = 0; j < orderListBox.Items.Count; j++)
                {
                    x = 1;
                    try
                    {
                        if (int.Parse(dataGridView2.SelectedRows[i].Cells[0].Value.ToString()) == int.Parse(orderListBox.Items[j].ToString().Split('\t')[0]))
                        {
                            x += int.Parse(orderListBox.Items[j].ToString().Split('\t')[2].Split('x')[1]);
                            orderListBox.Items.Add(dataGridView2.SelectedRows[i].Cells[0].Value.ToString() + '\t' + dataGridView2.SelectedRows[i].Cells[3].Value.ToString() + "\t" + " x " + x.ToString());
                            orderListBox.Items.RemoveAt(j);
                            noMatch = 1;
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        return;
                    }

                }
                if (noMatch == 0)
                {
                    orderListBox.Items.Add(dataGridView2.SelectedRows[i].Cells[0].Value.ToString() + '\t' + dataGridView2.SelectedRows[i].Cells[3].Value.ToString() + "\t" + " x " + '1');
                }

            }

        }

        private void updateTotalPrice()
        {
            // Updates the total Price label
            int totalPrice = 0;
            if (cartListBox.Items.Count != 0)
            {
                foreach (string product in cartListBox.Items)
                {
                    try
                    {
                        totalPrice += int.Parse(product.Split('\t')[2].Split('x')[0]) * int.Parse(product.Split('\t')[2].Split('x')[1]);
                    }
                    catch (Exception)
                    {

                        return;
                    }

                }
                TotalLabel.Text = "Total: " + totalPrice.ToString();
            }
            else
            {
                TotalLabel.Text = "Total: 0";
            }

        }

        private void printReceipt(ListBox listbox)
        {
            ListBox tmpListBox = new ListBox();
            tmpListBox = listbox;
            /* Prints the last receipt using the default printer or microsoft print to pdf printer */
            if (lastReceipt == string.Empty)
                return;

            DateTime dateTime = DateTime.Now;
            if (lastReceipt == " ")
            {
                foreach (string product in tmpListBox.Items)
                {
                    lastReceipt += $"{product} \n";
                }
                lastReceipt += $"{TotalLabel.Text} \n{dateTime} \nThank you for shoping with us!\nAW Supermarket";
            }

            PrintDocument pd = new PrintDocument();
            pd.PrintPage += new PrintPageEventHandler(PrintPage);
            if (!pd.PrinterSettings.IsValid)
            {
                pd.PrinterSettings.PrinterName = "Microsoft Print to PDF"; // Print to PDF
                pd.PrinterSettings.PrintToFile = true; // Save to file instead of printing
            }

            pd.Print();
        }

        public Button getSyncButton()
        {
            // returns the sync Now Button 
            // This is used to change the style of the button from the controller.
            return syncNowButton;
        }

        private void drawChart()
        {
            // Draws the chart on the splitcontainer panel.
            List<string> x = new List<string>();
            List<string> y = new List<string>();
            ChartArea chartArea = new ChartArea();

            // Clear the Lists on every call 
            x.Clear();
            y.Clear();

            // Clear the chart Area and series then add new every time.
            historyChart.ChartAreas.Clear();
            historyChart.Series.Clear();
            historyChart.ChartAreas.Add(chartArea);
            historyChart.Series.Add("1");

            // The x axis will allways have the time, Set title to Time
            historyChart.ChartAreas[0].AxisX.Title = "Time";
            x = controller.loadSeries(dataGridView3.SelectedRows[0].Cells[0].Value.ToString(), "date");
            if (x.Count == 0)
            {
                MessageBox.Show("This Product, does not have any history!");
                return;
            }

            if (pricePlotRadioButton.Checked)
            {
                y = controller.loadSeries(dataGridView3.SelectedRows[0].Cells[0].Value.ToString(), "price");
                historyChart.ChartAreas[0].AxisY.Title = "Price";
            }
            else if (stockPlotRadionButton.Checked)
            {
                y = controller.loadSeries(dataGridView3.SelectedRows[0].Cells[0].Value.ToString() ,"stock");
                historyChart.ChartAreas[0].AxisY.Title = "Stock";
            }

            // Add data points to the chart series
            for (int i = 0; i < x.Count; i++)
            {
                historyChart.Series["1"].Points.AddXY(x[i], int.Parse(y[i]));
            }
            
            
            // Set the chart type to line and the bg color to light green
            historyChart.Series["1"].ChartType = SeriesChartType.Line;
            historyChart.BackColor = Color.LightGreen;

            
            // Set x,y axis limits            
            historyChart.ChartAreas[0].AxisX.Minimum = 0;
            historyChart.ChartAreas[0].AxisX.Maximum = x.Count;
            historyChart.ChartAreas[0].AxisY.Minimum = getMinMaxY(y, "min");
            historyChart.ChartAreas[0].AxisY.Maximum = getMinMaxY(y, "max");
            
            // Add the chart to the split container panel1
            chartSplitContainer.Panel1.Controls.Add(historyChart);
        }

        private int getMinMaxY(List<string> y, string v)
        {
            // Calculates the min and max values of the Y List (Price or Stock)
            int result;
            try
            {
                result = int.Parse(y[0]);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error!" + e.Message);
                result = 0;
                return result;
            }
            
            if (v == "min")
            {
                for (int i = 0; i < y.Count; i++)
                {
                    try
                    {
                        if (int.Parse(y[i]) < result)
                        {
                            result = int.Parse(y[i]);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Error!" + e.Message);
                        result = 0;
                        return result;
                    }
                    
                }
            }
            else
            {
                for (int i = 0; i < y.Count; i++)
                {
                    try
                    {
                        if (int.Parse(y[i]) > result)
                        {
                            result = int.Parse(y[i]);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Error!" + e.Message);
                        result = 0;
                        return result;
                    }
                    
                }
            }
            return result;
        }
    }
}
