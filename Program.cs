// Program designed to return stock value information

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq; // To work with JSON
using Microsoft.Data.Sqlite; // Importing the SQLite package

class Program {
    // Create a new database
    static void CreateDatabase() {
        // Create a new SQLite database file called stocks.db and established the connection
        using var connection = new SqliteConnection("Data Source = stocks.db");
        connection.Open();

        // A string containing an SQL query that defines the structure of the StockPrices table.
        string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS StockPrices (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Symbol TEXT NOT NULL,
                Price TEXT NOT NULL,
                Time TEXT NOT NULL
            )";

        // SqliteCommand: Creates an SQL command with the specified query and connection.
        using var command = new SqliteCommand(createTableQuery, connection); 
        // Run the SQL command
        command.ExecuteNonQuery();
    }

    // Store the data in the database
    static void SaveToDatabase(string symbol, string price, string time) {
        // Create a new SQLite database file called stocks.db and established the connection
        using var connection = new SqliteConnection("Data Source = stocks.db");
        connection.Open();

        // Insert stock data into StockPrices.
        string insertDataQuery = @"
            INSERT INTO StockPrices (Symbol, Price, Time)
            VALUES (@symbol, @price, @time)";

        // Uses parameters (@symbol, @price, @time) to prevent SQL injection.
        using var command = new SqliteCommand(insertDataQuery, connection);
        command.Parameters.AddWithValue("@symbol", symbol);
        command.Parameters.AddWithValue("@price", price);
        command.Parameters.AddWithValue("@time", time);

        // Runs ExecuteNonQuery() to save the record.
        command.ExecuteNonQuery();

        // Confirm through the console that data was saved to the database
        Console.WriteLine($"Saved {symbol} price at {time}: ${price} to the database");    
    }

    // Show the Saved Data in the current database
    static void ShowSavedData() {
        // Create a new SQLite database file called stocks.db and established the connection
        using var connection = new SqliteConnection("Data Source = stocks.db");
        connection.Open();

        // Select all data from the StockPrices table
        string selectDataQuery = @"
            SELECT * FROM StockPrices";

        // Create a new SqliteCommand with the selectDataQuery and connection
        using var command = new SqliteCommand(selectDataQuery, connection);

        // Execute the command and store the result in a SQLiteDataReader
        using var reader = command.ExecuteReader();

        // Loop through the reader and print the data to the console
        while (reader.Read()) {
            Console.WriteLine($"Symbol: {reader["Symbol"]}, Price: {reader["Price"]}, Time: {reader["Time"]}");
        }
    }

    // Retrieve stock data from API
    static async Task RetrieveStockData(string symbol) {
        string apiKey = "SIIUQLPHK5GE24CP";
        string url = $@"https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol={symbol}&interval=5min&apikey={apiKey}";
        
        using HttpClient client = new HttpClient();
        var response = await client.GetStringAsync(url);

        // Parse the JSON Data
        try {
            JObject data = JObject.Parse(response); // parse the JSON data
            var timeSeries = data["Time Series (5min)"]; // Extract the time series data

            // check that the time series data is not null
            if (timeSeries != null && timeSeries.First != null) {
                // if not null then take the most recent stock entry
                var latestEntry = timeSeries.First;
                
                if (latestEntry != null && latestEntry.First != null) {
                    var pathParts = latestEntry.Path.Split('\'');
                    
                    // check that the path has at least 3 parts and extract last part
                    if (pathParts.Length >= 3) {
                        string time = pathParts[3];
                        // get the opening price
                        var openPriceToken = latestEntry.First["1. open"];
                        // check that the opening price is not null
                        if (openPriceToken != null) {
                            string latestPrice = openPriceToken.ToString();
                            // Print to the Console a line that says
                            // "Latest {symbol} price at {time}: ${latest price}"
                            Console.WriteLine($"Latest {symbol} price at {time}: ${latestPrice}");
                            // Save data to the database
                            SaveToDatabase(symbol, latestPrice, time);
                        } else {
                            Console.WriteLine("Error: Opening price not found");
                        }
                    } else {
                        Console.WriteLine("Error: Invalid path format");
                    }
                } else {
                    Console.WriteLine("Error: Latest entry data not found");
                }
            } else {
                Console.WriteLine("Error: Time Series data not found");
            }
        } 
        // if the data can't be parsed just print an error message
        catch (Exception ex) {
            Console.WriteLine($"Error Processing data: {ex.Message}");
        }
    }

    // Main Program
    static async Task Main() {
        // Create the database
        CreateDatabase();

        Console.WriteLine("Choose an option:");
        Console.WriteLine("1. Show all previous entries");
        Console.WriteLine("2. Perform a new API query");

        var choice = Console.ReadLine();

        if (choice == "1") {
            ShowSavedData();
        } else if (choice == "2") {
            Console.WriteLine("Enter the stock symbol:");
            string symbol = Console.ReadLine();
            await RetrieveStockData(symbol);
        } else {
            Console.WriteLine("Invalid choice");
        }
    }
}