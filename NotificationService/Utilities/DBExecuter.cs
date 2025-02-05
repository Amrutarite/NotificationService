using Dapper;
using Microsoft.Data.SqlClient;
using NotificationService.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace NotificationService.Utilities
{
    public class DBExecutor
    {
        private readonly IDbConnection _dbConnection;

        // Constructor to inject the database connection
        public DBExecutor(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        // Method to get notifications by status
        public async Task<IEnumerable<EmailNotification>> GetNotificationsByStatusAsync(string status)
        {
            // Define query to fetch notifications by status
            string query = "SELECT * FROM EmailNotifications WHERE Status = @Status";
            return await _dbConnection.QueryAsync<EmailNotification>(query, new { Status = status });
        }

        // Method to get a notification by ID
        public async Task<EmailNotification> GetNotificationByIdAsync(int id)
        {
            // Define query to fetch notification by ID
            string query = "SELECT * FROM EmailNotifications WHERE Id = @Id";
            return await _dbConnection.QueryFirstOrDefaultAsync<EmailNotification>(query, new { Id = id });
        }

        // Method to execute a query (INSERT, UPDATE, DELETE, etc.)
        public async Task<int> ExecuteAsync(string query, object parameters = null)
        {
            // Execute non-SELECT queries like INSERT, UPDATE, DELETE
            return await _dbConnection.ExecuteAsync(query, parameters);
        }

        // Method to execute a query and return a scalar value (e.g., an ID)
        public async Task<T> ExecuteScalarAsync<T>(string query, object parameters = null)
        {
            // Execute a query and return a scalar value
            return await _dbConnection.ExecuteScalarAsync<T>(query, parameters);
        }

        // Method to execute a query and return a list of results (e.g., SELECT)
        public async Task<IEnumerable<T>> QueryAsync<T>(string query, object parameters = null)
        {
            // Execute a query and return a list of results
            return await _dbConnection.QueryAsync<T>(query, parameters);
        }
    }
}
