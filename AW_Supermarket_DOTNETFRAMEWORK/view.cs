using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AW_Supermarket_DOTNETFRAMEWORK
{
    public partial class mainForm : Form
    {
        Controller controller;
        string lastReceipt;
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
            updateDataGridView();

        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            Font font = new Font("Arial", 12);
            SolidBrush brush = new SolidBrush(Color.Black);
            g.DrawString(lastReceipt, font, brush, 0, 0);
        }

        private void productTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateTextboxesEnabledStatus(productTypeComboBox.SelectedIndex);
        }

        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            controller.FormColsing();
        }

        private void addToCartButton_Click(object sender, EventArgs e)
        {
            updateCart();
        }

        private void sellButton_Click(object sender, EventArgs e)
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
            // Sell then print receipt
            controller.sell_returnButtonPressed(cartListBox, "sell");
            lastReceipt = " ";
            printReceipt(cartListBox);
            cartListBox.Items.Clear();
            updateDataGridView();
            TotalLabel.Text = "Total: 0";
        }

        private void removeFromCartButton_Click(object sender, EventArgs e)
        {
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
            updateNewOrderList();
        }

        private void RemoveFromOrderButton_Click(object sender, EventArgs e)
        {
            if (orderListBox.SelectedIndex == -1)
                return;
            orderListBox.Items.RemoveAt(orderListBox.SelectedIndex);
        }

        private void orderNowButton_Click(object sender, EventArgs e)
        {
            if (orderListBox.Items.Count == 0)
                return;
            controller.orderNowButtonPressed(orderListBox);
            orderListBox.Items.Clear();
            updateDataGridView();
        }

        private void returnButton_Click(object sender, EventArgs e)
        {
            if (cartListBox.Items.Count == 0)
                return;
            controller.sell_returnButtonPressed(cartListBox, "return");
            lastReceipt = " ";
            printReceipt(cartListBox);
            cartListBox.Items.Clear();
            updateDataGridView();
            TotalLabel.Text = "Total: 0";
        }

        private void searchTextBox_TextChanged(object sender, EventArgs e)
        {
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
            MessageBox.Show(controller.top10AndTotalSales(yearRadioButton.Checked, true));
        }

        private void reprintButton_Click(object sender, EventArgs e)
        {
            printReceipt(cartListBox);
        }

        private void totalSalesButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show(controller.top10AndTotalSales(yearRadioButton.Checked, false));
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
            dataGridView1.DataSource = controller.getDataSource();
            dataGridView2.DataSource = controller.getDataSource();
            dataGridView1.Refresh();
            dataGridView2.Refresh();
        }

        private void clearTextBoxes()
        {
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
            /* Prints the last receipt using the default printer or microsoft print to pdf printer */
            if (lastReceipt == string.Empty)
                return;

            DateTime dateTime = DateTime.Now;
            if (lastReceipt == " ")
            {
                foreach (string product in listbox.Items)
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

        
        private async void syncNowButton_Click(object sender, EventArgs e)
        {
            syncNowButton.BackColor = Color.Yellow;
            apiTextBox.Enabled = false;
            if (controller.syncNowButtonPressed(apiTextBox.Text))
            {
                
                syncNowButton.BackColor = Color.LightGreen;
                updateDataGridView();
                await Task.Run(() =>
                {
                    Thread.Sleep(1500);
                });

            }
            else
            {
                syncNowButton.BackColor = Color.LightCoral;
                MessageBox.Show("Unable to sync!!\nPlease check your API or your internet connection.");
            }
            apiTextBox.Enabled = true;
            syncNowButton.BackColor = Color.Transparent;
        }

        private void autoSyncButton_Click(object sender, EventArgs e)
        {
            Thread autoSync = new Thread(() => controller.autoSyncButtonPressed(apiTextBox.Text));
            if (autoSyncButton.Text == "Auto Sync: Off")
            {
                autoSyncButton.Text = "Auto Sync: On";
                autoSyncButton.BackColor = Color.LightGreen;
                apiTextBox.Enabled = false;
                syncNowButton.Enabled = false;
                autoSync.Start();
            }
            else
            {
                autoSync.Abort();
                autoSyncButton.Text = "Auto Sync: Off";
                autoSyncButton.BackColor = Color.LightCoral;
                apiTextBox.Enabled = true;
                syncNowButton.Enabled = true;
                autoSyncRunning(false);
            }
        }
        public void autoSyncRunning(bool state)
        {
            if (state)
            {
                syncNowButton.Text = "Running...";
                syncNowButton.BackColor = Color.LightGreen;
            }
            else
            {
                syncNowButton.Text = "Sync Now";
                syncNowButton.BackColor = Color.Transparent;
            }
        }
    }
}
