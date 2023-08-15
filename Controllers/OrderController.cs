using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;





namespace OrderAPI.Controllers
{
    [ApiController]
    [EnableCors("MyPolicy")]
    public class OrderController : Controller
    {
        // RabbitMQ setup
        private readonly string rabbitUser = "student";
        private readonly string rabbitPassword = "student123";
        private readonly string rabbitHost = "studentdocker.informatika.uni-mb.si";
        private readonly string rabbitPort = "5672";
        private readonly string vhost = "/";
        private readonly string exchange = "upp-3";
        private readonly string routingKey = "zelovarnikey";

        // JWT setup
        private readonly string SECRET_KEY = "SUPER_STRONG_SECRET_BY_JAN";

        private void LogMessageToRabbitMQ(string correlationId, string message, string logType, string url, string applicationName)
        {
            try
            {
                var factory = new ConnectionFactory() { HostName = rabbitHost, Port = Convert.ToInt32(rabbitPort), UserName = rabbitUser, Password = rabbitPassword, VirtualHost = vhost };
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct, durable: true);

                    string msg = $"{DateTime.Now:O} {logType} {url} Correlation: {correlationId} [{applicationName}] - {message}";
                    var body = Encoding.UTF8.GetBytes(msg);

                    channel.BasicPublish(exchange: exchange, routingKey: routingKey, basicProperties: null, body: body);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging message to RabbitMQ: {ex.Message}");
            }
        }


        private async Task SendStatistics(Dictionary<string, string> data)
        {
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.PostAsJsonAsync("https://statistics-jeb4.onrender.com/add-statistic", data);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending statistics: {ex.Message}");
            }
        }

        public static class JwtAuthHelper
        {
            public static void JwtAuth(HttpContext httpContext, string SECRET_KEY)
            {
                // Comment out or remove this code
                /*
                var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Missing or invalid Authorization header");
                }
                var token = authHeader.Substring("bearer ".Length).Trim();

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(SECRET_KEY);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = false,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                try
                {
                    tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                }
                catch (Exception ex)
                {
                    throw new Exception("Invalid token", ex);
                }
                */

                // Add a debug log message
                Console.WriteLine("Skipping JWT token check...");
            }
        }




        //1
        [HttpGet] // Označuje, da je to HTTP GET zahtevek
        [Route("order/get/All")] // Določa URL pot, na kateri bo ta metoda dostopna
        public async Task<string> GetAllAsync()
        {
            JwtAuthHelper.JwtAuth(HttpContext, SECRET_KEY);


            string correlationId = Guid.NewGuid().ToString();

            LogMessageToRabbitMQ(correlationId, "Received a request to get all orders", "INFO", "/order/get/All", "order-service");

            await SendStatistics(new Dictionary<string, string> {
                { "service", "order-service" },
                { "endpoint", "/order/get/All" },
                { "method", "GET" },
                { "timestamp", DateTime.Now.ToString("o") }
            });

            //logger
            HttpClient clientHttp = new HttpClient();

            // Koda za interakcijo z MongoDB podatkovno bazo
            var client = new MongoClient("mongodb+srv://nikkljucevsek:OldbtLLbshDbB69v@cluster0.9uuzozi.mongodb.net/");
            var database = client.GetDatabase("ITA");
            var collection = database.GetCollection<Order>("Order");

            // Pridobivanje vseh dokumentov iz kolekcije
            var result = await collection.FindAsync(_ => true);

            // Pretvarjanje rezultatov v seznam objektov tipa Order
            List<Order> resultList = new List<Order>();
            foreach (var item in result.ToList())
            {
                resultList.Add(item);
            }

            // Vrne seznam naročil kot JSON niz
            return JsonConvert.SerializeObject(resultList);
        }

        //2
        [HttpGet]
        [Route("order/get/byID")]
        public async Task<string> GetByIDAsync(string Id)
        {
            JwtAuthHelper.JwtAuth(HttpContext, SECRET_KEY);


            string correlationId = Guid.NewGuid().ToString();

            LogMessageToRabbitMQ(correlationId, $"Received a request to get order by id {Id}", "INFO", "/order/get/byID", "order-service");

            await SendStatistics(new Dictionary<string, string> {
                { "service", "order-service" },
                { "endpoint", "/order/get/byID" },
                { "method", "GET" },
                { "timestamp", DateTime.Now.ToString("o") }
            });
            //logger
            HttpClient clientHttp = new HttpClient();
            var client = new MongoClient("mongodb+srv://nikkljucevsek:OldbtLLbshDbB69v@cluster0.9uuzozi.mongodb.net/");
            var database = client.GetDatabase("ITA");
            var collection = database.GetCollection<Order>("Order");

            // Pretvarjanje ID-ja iz niza v ObjectId
            var id = new ObjectId(Id);

            // Pridobivanje vseh dokumentov iz kolekcije (enako kot v prejšnji metodi)
            var result = await collection.FindAsync(_ => true);

            // Pretvarjanje rezultatov v seznam objektov tipa Order
            List<Order> resultList = new List<Order>();
            foreach (var item in result.ToList())
            {
                resultList.Add(item);
            }

            // Vrne naročilo z določenim ID-jem kot JSON niz
            return JsonConvert.SerializeObject(resultList.Find(item => item._id == id));
        }

        //3
        [HttpGet]
        [Route("order/get/latest")]
        public async Task<string> GetLatestAsync()
        {
            JwtAuthHelper.JwtAuth(HttpContext, SECRET_KEY);


            string correlationId = Guid.NewGuid().ToString();

            LogMessageToRabbitMQ(correlationId, "Received a request to get the latest order", "INFO", "/order/get/latest", "order-service");

            await SendStatistics(new Dictionary<string, string> {
        { "service", "order-service" },
        { "endpoint", "/order/get/latest" },
        { "method", "GET" },
        { "timestamp", DateTime.Now.ToString("o") }
         });

            HttpClient clientHttp = new HttpClient();

            var client = new MongoClient("mongodb+srv://nikkljucevsek:OldbtLLbshDbB69v@cluster0.9uuzozi.mongodb.net/");
            var database = client.GetDatabase("ITA");
            var collection = database.GetCollection<Order>("Order");

            var result = await collection.FindAsync(_ => true);

            List<Order> resultList = new List<Order>();
            foreach (var item in result.ToList())
            {
                resultList.Add(item);
            }

            if (resultList.Any())
            {
                LogMessageToRabbitMQ(correlationId, "Successfully got the latest order", "INFO", "/order/get/latest", "order-service");

                return JsonConvert.SerializeObject(resultList.Last());
            }
            else
            {
                LogMessageToRabbitMQ(correlationId, "No orders found", "INFO", "/order/get/latest", "order-service");

                return "No orders found";
            }
        }


        //4
        [HttpDelete] // Označuje, da je to HTTP DELETE zahtevek
        [Route("order/delete/byID")] // Določa URL pot, na kateri bo ta metoda dostopna
        public async Task<string> DeleteByIDAsync(string Id)
        {
            JwtAuthHelper.JwtAuth(HttpContext, SECRET_KEY);


            string correlationId = Guid.NewGuid().ToString(); // Generate a new correlation ID

            LogMessageToRabbitMQ(correlationId, "Received a request to delete an order", "INFO", "/order/delete/byID", "order-service"); // Log the received request to RabbitMQ

            await SendStatistics(new Dictionary<string, string> {
        { "service", "order-service" },
        { "endpoint", "/order/delete/byID" },
        { "method", "DELETE" },
        { "timestamp", DateTime.Now.ToString("o") }
         }); // Send statistics

            HttpClient clientHttp = new HttpClient();

            var client = new MongoClient("mongodb+srv://nikkljucevsek:OldbtLLbshDbB69v@cluster0.9uuzozi.mongodb.net/");
            var database = client.GetDatabase("ITA");
            var collection = database.GetCollection<Order>("Order");

            // Convert the ID from string to ObjectId
            var id = new ObjectId(Id);

            // Delete the document with the matching ID from the database
            var result = await collection.DeleteOneAsync(item => item._id == id);

            // Log the successful deletion to RabbitMQ
            LogMessageToRabbitMQ(correlationId, "Successfully deleted the order", "INFO", "/order/delete/byID", "order-service");

            // Return a message indicating successful deletion
            return "Uspešno odstranjen!";
        }


        //5
        [HttpDelete] // Indicates that this is an HTTP DELETE request
        [Route("order/delete/latest")] // Specifies the URL path at which this method will be accessible
        public async Task<string> DeleteLatestAsync()
        {
            JwtAuthHelper.JwtAuth(HttpContext, SECRET_KEY);


            string correlationId = Guid.NewGuid().ToString(); // Generate a new correlation ID

            LogMessageToRabbitMQ(correlationId, "Received a request to delete the latest order", "INFO", "/order/delete/latest", "order-service"); // Log the received request to RabbitMQ

            await SendStatistics(new Dictionary<string, string> {
        { "service", "order-service" },
        { "endpoint", "/order/delete/latest" },
        { "method", "DELETE" },
        { "timestamp", DateTime.Now.ToString("o") }
    }); // Send statistics

            var client = new MongoClient("mongodb+srv://nikkljucevsek:OldbtLLbshDbB69v@cluster0.9uuzozi.mongodb.net/");
            var database = client.GetDatabase("ITA");
            var collection = database.GetCollection<Order>("Order");

            // Fetch all documents from the collection
            var result = await collection.FindAsync(_ => true);

            // Convert the results to a list of Order objects
            List<Order> resultList = new List<Order>();
            foreach (var item in result.ToList())
            {
                resultList.Add(item);
            }

            // Get the ID of the last item on the list
            var id = new ObjectId(resultList.Last()._id.ToString());

            // Delete the document with the matching ID from the database
            var result2 = await collection.DeleteOneAsync(x => x._id == id);

            // Log the successful deletion to RabbitMQ
            LogMessageToRabbitMQ(correlationId, "Successfully deleted the latest order", "INFO", "/order/delete/latest", "order-service");

            // Return a message indicating successful deletion
            return "Uspešno odstranjen!";
        }


        //6
        [HttpPost]
        [Route("order/post/order")]
        public async Task<string> PostOrderAsync(Order order)
        {
            JwtAuthHelper.JwtAuth(HttpContext, SECRET_KEY);


            string correlationId = Guid.NewGuid().ToString(); // Generate a new correlation ID

            LogMessageToRabbitMQ(correlationId, "Received a request to post an order", "INFO", "/order/post/order", "order-service"); // Log the received request to RabbitMQ

            await SendStatistics(new Dictionary<string, string> {
        { "service", "order-service" },
        { "endpoint", "/order/post/order" },
        { "method", "POST" },
        { "timestamp", DateTime.Now.ToString("o") }
    }); // Send statistics

            try
            {
                // Make an HTTP request to get the users data
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri("http://localhost:8000/"); // Replace with the correct URL of the user data API
                    HttpResponseMessage response = await httpClient.GetAsync("users");
                    response.EnsureSuccessStatusCode();

                    // Deserialize the response into a list of User objects
                    List<User> usersData = await response.Content.ReadAsAsync<List<User>>();

                    // Check if the specified user_id exists in the fetched data
                    bool userExists = usersData.Any(user => user.id == order.userId);

                    if (!userExists)
                    {
                        LogMessageToRabbitMQ(correlationId, "User not found", "ERROR", "/order/post/order", "order-service"); // Log the error to RabbitMQ
                        return "User not found";
                    }
                }

                var client = new MongoClient("mongodb+srv://nikkljucevsek:OldbtLLbshDbB69v@cluster0.9uuzozi.mongodb.net/");
                var database = client.GetDatabase("ITA");
                var collection = database.GetCollection<Order>("Order");

                // Insert the order into the database
                await collection.InsertOneAsync(order);

                LogMessageToRabbitMQ(correlationId, "Order successfully added", "INFO", "/order/post/order", "order-service"); // Log the successful addition to RabbitMQ

                // Return a success message
                return "Order successfully added!";
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occurred during the process
                // Log the error and return an error message
                LogMessageToRabbitMQ(correlationId, $"Error when creating order: {ex.Message}", "ERROR", "/order/post/order", "order-service"); // Log the error to RabbitMQ
                return "Error when creating order";
            }
        }


        //7
        [HttpPut]
        [Route("order/put/order")]
        public async Task<string> PutOrderAsync(Update update)
        {
            JwtAuthHelper.JwtAuth(HttpContext, SECRET_KEY);


            string correlationId = Guid.NewGuid().ToString(); // Generate a new correlation ID

            LogMessageToRabbitMQ(correlationId, "Received a request to update an order", "INFO", "/order/put/order", "order-service"); // Log the received request to RabbitMQ

            await SendStatistics(new Dictionary<string, string> {
        { "service", "order-service" },
        { "endpoint", "/order/put/order" },
        { "method", "PUT" },
        { "timestamp", DateTime.Now.ToString("o") }
    }); // Send statistics

            var client = new MongoClient("mongodb+srv://nikkljucevsek:OldbtLLbshDbB69v@cluster0.9uuzozi.mongodb.net/");
            var database = client.GetDatabase("ITA");
            var collection = database.GetCollection<Order>("Order");

            var resultDelete = await collection.DeleteOneAsync(item => item._id == new ObjectId(update.Id));

            if (resultDelete.DeletedCount == 0)
            {
                LogMessageToRabbitMQ(correlationId, "Order not found", "ERROR", "/order/put/order", "order-service"); // Log the error to RabbitMQ
                return "Order not found";
            }

            update.Order._id = new ObjectId(update.Id);
            await collection.InsertOneAsync(update.Order);

            LogMessageToRabbitMQ(correlationId, "Order successfully updated", "INFO", "/order/put/order", "order-service"); // Log the successful update to RabbitMQ

            return "Uspešno posodobljen!";
        }


        //8
        [HttpPut]
        [Route("order/put/latest")]
        public async Task<string> PutLatestOrderAsync(Order order)
        {
            JwtAuthHelper.JwtAuth(HttpContext, SECRET_KEY);


            string correlationId = Guid.NewGuid().ToString(); // Generate a new correlation ID

            LogMessageToRabbitMQ(correlationId, "Received a request to update the latest order", "INFO", "/order/put/latest", "order-service"); // Log the received request to RabbitMQ

            await SendStatistics(new Dictionary<string, string> {
        { "service", "order-service" },
        { "endpoint", "/order/put/latest" },
        { "method", "PUT" },
        { "timestamp", DateTime.Now.ToString("o") }
    }); // Send statistics

            var client = new MongoClient("mongodb+srv://nikkljucevsek:OldbtLLbshDbB69v@cluster0.9uuzozi.mongodb.net/");
            var database = client.GetDatabase("ITA");
            var collection = database.GetCollection<Order>("Order");

            var result = await collection.FindAsync(_ => true);

            List<Order> resultList = new List<Order>();
            foreach (var item in result.ToList())
            {
                resultList.Add(item);
            }

            if (resultList.Count == 0)
            {
                LogMessageToRabbitMQ(correlationId, "No orders found", "ERROR", "/order/put/latest", "order-service"); // Log the error to RabbitMQ
                return "No orders found";
            }

            var lastOrder = resultList.Last();
            var resultDelete = await collection.DeleteOneAsync(item => item._id == lastOrder._id);

            if (resultDelete.DeletedCount == 0)
            {
                LogMessageToRabbitMQ(correlationId, "Failed to update the latest order", "ERROR", "/order/put/latest", "order-service"); // Log the error to RabbitMQ
                return "Failed to update the latest order";
            }

            order._id = lastOrder._id;
            await collection.InsertOneAsync(order);

            LogMessageToRabbitMQ(correlationId, "Successfully updated the latest order", "INFO", "/order/put/latest", "order-service"); // Log the successful update to RabbitMQ

            return "Uspešno posodobljen!";
        }


        //9
        [HttpGet]
        [Route("order/get/byUserId")]
        public async Task<string> GetByCustomerIdAsync(string userId)
        {
            JwtAuthHelper.JwtAuth(HttpContext, SECRET_KEY);


            string correlationId = Guid.NewGuid().ToString(); // Generate a new correlation ID

            LogMessageToRabbitMQ(correlationId, "Received a request to get orders by user ID", "INFO", "/order/get/byUserId", "order-service"); // Log the received request to RabbitMQ

            await SendStatistics(new Dictionary<string, string> {
        { "service", "order-service" },
        { "endpoint", "/order/get/byUserId" },
        { "method", "GET" },
        { "timestamp", DateTime.Now.ToString("o") }
    }); // Send statistics

            var client = new MongoClient("mongodb+srv://nikkljucevsek:OldbtLLbshDbB69v@cluster0.9uuzozi.mongodb.net/");
            var database = client.GetDatabase("ITA");
            var collection = database.GetCollection<Order>("Order");

            var filter = Builders<Order>.Filter.Eq("userId", userId);
            var result = await collection.FindAsync(filter);

            List<Order> resultList = await result.ToListAsync();

            if (resultList.Count == 0)
            {
                LogMessageToRabbitMQ(correlationId, "No orders found for the user", "INFO", "/order/get/byUserId", "order-service"); // Log the error to RabbitMQ
                return "No orders found for the user";
            }

            LogMessageToRabbitMQ(correlationId, "Successfully retrieved orders by user ID", "INFO", "/order/get/byUserId", "order-service"); // Log the successful retrieval to RabbitMQ

            return JsonConvert.SerializeObject(resultList);
        }


        public class Update
    {
        public Order Order { get; set; }
        public string Id { get; set; }
    }
        public class User
        {
            public string id { get; set; }
            public string username { get; set; }
            public string password { get; set; }
            public string name { get; set; }
            public string surname { get; set; }
            public string type { get; set; }
        }
    }

}