using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AW_Supermarket_DOTNETFRAMEWORK
{
    internal class ProductList
    {
        /* Delarations */
        BindingList<Product> productList;
        BindingSource productlistSource;
        private List<string> csvFile;

        // Used as min and max boundaries to the product id random generator
        private const int MIN_PRODUCT_ID = 0, MAX_PRODUCT_ID = 99999;
        public ProductList()
        {
            productList = new BindingList<Product>();
            productlistSource = new BindingSource();
            productlistSource.DataSource = productList;
            try
            {
                loadCSVFile();
            }
            catch (Exception)
            {
                return;
            }
        }

        internal List<string> getAllSoldProducts()
        {
            /* Loads all the products that were sold from the database (database1.csv) file.
             * Returns as a list of strings */
            List<string> soldDatabase = new List<string>();
            if (File.Exists("database1.csv"))
            {
                soldDatabase = System.IO.File.ReadAllText("database1.csv").Split('\r').ToList();
                soldDatabase.RemoveAt(soldDatabase.Count() - 1);
                return soldDatabase;
            }
            else
            {
                File.Create("database.csv").Close();
                return soldDatabase;
            }
        }

        private void loadCSVFile()
        {
            /* loadCSVFile function -> loads a csv file (database.csv) which includes all information about the products in the supermarket.
            * The function loads the info to the product list and checks if a product does not have a product id (may be cased be adding the products manualy to the csv file) it requests a 
            * new unique product ID. 
            * If the file is missing a new file gets created!
            */
            if (File.Exists("database.csv"))
            {
                csvFile = new List<string>();
                try
                {
                    csvFile = System.IO.File.ReadAllText("database.csv").Split('\r').ToList();
                }
                catch (Exception)
                {
                    return;
                }

                // Remove the last line which is an empty line!
                csvFile.RemoveAt(csvFile.Count() - 1);

                foreach (string line in csvFile)
                {
                    try
                    {
                        productList.Add(new Product
                        {
                            ProductID = int.Parse(line.Split(',').ElementAt(0)),
                            Name = line.Split(',').ElementAt(1),
                            Price = int.Parse(line.Split(',').ElementAt(2)),
                            Author = line.Split(',').ElementAt(3),
                            Genre = line.Split(',').ElementAt(4),
                            Format = line.Split(',').ElementAt(5),
                            Language = line.Split(',').ElementAt(6),
                            Platform = line.Split(',').ElementAt(7),
                            PlayTime = line.Split(',').ElementAt(8),
                            Quantity = int.Parse(line.Split(',').ElementAt(9)),
                            Type = line.Split(',').Last()
                        });
                    }
                    catch (Exception)
                    {

                        return;
                    }


                    if (line.Split(',').ElementAt(0) == "")
                    {
                        productList.ElementAt(productList.Count).ProductID = generateProductID();
                    }

                }

            }
            else
            {
                File.Create("database.csv").Close();
            }
        }

        internal int generateProductID()
        {
            /* This function generates a new random product ID and to make it unique it compares it to all other product id's */
            Random rand = new Random();
            int productID = rand.Next(MIN_PRODUCT_ID, MAX_PRODUCT_ID);
            for (int i = 0; i < productList.Count; i++)
            {
                if (productList.ElementAt(i).ProductID == productID)
                {
                    productID = rand.Next(MIN_PRODUCT_ID, MAX_PRODUCT_ID);
                    i = 0;
                }
            }
            return productID;
        }

        internal object getDataSource()
        {
            // Returns the dataSource 
            return productlistSource;
        }

        internal void saveFile()
        {
            /* Saves all the data in the productList to the file/database (database.csv) */
            string result = "";

            foreach (var product in productList)
            {
                result +=

                    product.ProductID.ToString() +
                    ',' +
                    product.Name +
                    ',' +
                    product.Price.ToString() +
                    ',' +
                    product.Author +
                    ',' +
                    product.Genre +
                    ',' +
                    product.Format +
                    ',' +
                    product.Language +
                    ',' +
                    product.Platform +
                    ',' +
                    product.PlayTime +
                    ',' +
                    product.Quantity +
                    ',' +
                    product.Type +

                    '\r';
            }
            if (!File.Exists("database.csv"))
            {
                File.Create("database.csv").Close();
            }
            System.IO.File.WriteAllText("database.csv", result);
        }

        internal void SaveSold()
        {
            /* Saves the number of sold products with product id and date in the productList to the file/database (database1.csv) */
            string result = "";
            string year = DateTime.Now.Year.ToString();
            string month = DateTime.Now.Month.ToString();
            foreach (var product in productList)
            {
                if (product.sold != 0)
                {
                    result +=
                    year +
                    ',' +
                    month +
                    ',' +
                    product.ProductID.ToString() +
                    ',' +
                    product.Name +
                    ',' +
                    product.sold.ToString() +

                    '\r';
                }
                product.sold = 0;
            }
            if (!File.Exists("database1.csv"))
                File.Create("database1.csv").Close();


            System.IO.File.AppendAllText("database1.csv", result);


        }

        internal void addProduct(int productID, string name, int price, string author, string genre, string format, string language, string platform, string playtime, int inventory, string productType)
        {
            /* Adds a new Product to the productList */
            productList.Add(new Product
            {
                ProductID = productID,
                Name = name,
                Price = price,
                Author = author,
                Genre = genre,
                Format = format,
                Language = language,
                Platform = platform,
                PlayTime = playtime,
                Quantity = inventory,
                Type = productType,
                sold = 0
            }); ;
        }

        internal bool isProductIDValid(int productID)
        {
            /* Checks if the product ID is used by another product */
            foreach (var product in productList)
            {
                if (productID == product.ProductID)
                {
                    return false;
                }
            }
            return true;
        }

        internal void deleteProduct(int productID)
        {
            /* Deletes a product from the productList */
            for (int i = 0; i < productList.Count; i++)
            {
                if (productID == productList.ElementAt(i).ProductID)
                {
                    productList.RemoveAt(i);
                }
            }


        }

        internal void updateQuantity(object item, string operation)
        {
            /* Edits the Quantity of a product 
               this function increases or decreases the quantity based on the operation that is being preformed "sell or return"*/
            if (operation == "sell")
            {
                for (int i = 0; i < productList.Count; i++)
                {
                    try
                    {
                        if (productList.ElementAt(i).ProductID == int.Parse(item.ToString().Split('\t')[0]))
                        {
                            productList.ElementAt(i).Quantity -= int.Parse(item.ToString().Split('\t')[2].Split('x')[1]);
                        }
                    }
                    catch (Exception)
                    {

                        return;
                    }

                }
            }
            else if (operation == "return")
            {
                for (int i = 0; i < productList.Count; i++)
                {
                    try
                    {
                        if (productList.ElementAt(i).ProductID == int.Parse(item.ToString().Split('\t')[0]))
                        {
                            productList.ElementAt(i).Quantity += int.Parse(item.ToString().Split('\t')[2].Split('x')[1]);
                        }
                    }
                    catch (Exception)
                    {

                        return;
                    }

                }
            }

        }

        internal int getQuantity(int productID)
        {
            /* Returns the quantity of a specific product! */
            foreach (var product in productList)
            {
                if (product.ProductID == productID)
                {
                    return product.Quantity;
                }
            }
            return 0;
        }

        internal void updateSold(object item, string operation)
        {
            /* Update the number of the variable (Sold) based on the operation that is being preformed */
            if (operation == "sell")
            {
                for (int i = 0; i < productList.Count; i++)
                {
                    try
                    {
                        if (productList.ElementAt(i).ProductID == int.Parse(item.ToString().Split('\t')[0]))
                        {
                            productList.ElementAt(i).sold += int.Parse(item.ToString().Split('\t')[2].Split('x')[1]);
                        }
                    }
                    catch (Exception)
                    {
                        return;
                    }

                }
            }
            else if (operation == "return")
            {
                for (int i = 0; i < productList.Count; i++)
                {
                    try
                    {
                        if (productList.ElementAt(i).ProductID == int.Parse(item.ToString().Split('\t')[0]))
                        {
                            productList.ElementAt(i).sold -= int.Parse(item.ToString().Split('\t')[2].Split('x')[1]);
                        }
                    }
                    catch (Exception)
                    {
                        return;
                    }

                }
            }
        }

        internal List<Product> getProducts()
        {
            /* Returns all the products in the supermarket as a list of type Product */
            List<Product> products = new List<Product>();
            foreach (var product in productList)
            {
                products.Add(product);
            }
            return products;
        }
    }
}