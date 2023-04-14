namespace AW_Supermarket_DOTNETFRAMEWORK
{
    internal class Product
    {
        /* Product class includes 
         * ProductID : which is a unique id for each product.
         * Quantity : Is the quantity of the product in the inventory of the supermaket
         * Type : Is the product type for example (Book, file or game)
         * Name: is the name of the product
         * Price : Is the price of the product
         * Author: The author of a product of type book
         * Genre: is the genre of a product of type book
         * Format: is the format of a product of type game or file
         * Platform: is the platform for a specific game
         * PlayTime: Is the playTime of a specific film
         * sold : is the quantity of a specific product that have been sold
         */
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public string Author { get; set; }
        public string Genre { get; set; }
        public string Format { get; set; }
        public string Language { get; set; }
        public string Platform { get; set; }
        public string PlayTime { get; set; }

        public int sold { get; set; }
    }
}