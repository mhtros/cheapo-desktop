using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using Cheapo.Models;
using Dapper;

// Copyright (c) 2021 Panagiotis Mitropanos
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace Cheapo.Database
{
    public static class SqlLiteDataAccess
    {
        private static string LoadConnectionString(string id = "Default")
        {
            return ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }

        public static IEnumerable<PurchaseModel> LoadPurchasesByYear(string year)
        {
            using IDbConnection cnn = new SQLiteConnection(LoadConnectionString());
            var output = cnn
                .Query<PurchaseModel>("select * from Purchases where Year = @Year", new { Year = year });

            return output.ToList();
        }

        public static IEnumerable<PurchaseModel> LoadPurchasesByYearAndMonth(string year, string month)
        {
            using IDbConnection cnn = new SQLiteConnection(LoadConnectionString());
            var output = cnn
                .Query<PurchaseModel>(
                    "select * from Purchases where Year = @Year and Month = @Month"
                    , new { Year = year, Month = month }
                );

            return output.ToList();
        }

        public static void InsertPurchases(PurchaseModel purchase)
        {
            using IDbConnection connection = new SQLiteConnection(LoadConnectionString());
            connection.Execute(
                "insert into Purchases(Description, Price, Year, Month) values (@Description, @Price, @Year, @Month)",
                purchase);
        }

        public static void UpdatePurchases(PurchaseModel purchase)
        {
            var id = purchase.Id;
            var price = purchase.Price;
            var description = purchase.Description;

            using IDbConnection connection = new SQLiteConnection(LoadConnectionString());
            connection.Query<PurchaseModel>(
                "update Purchases set Description = @Description, Price = @Price where Id = @Id",
                new { Description = description, Price = price, Id = id });
        }

        public static void DeletePurchases(PurchaseModel purchase)
        {
            var id = purchase.Id;
            using IDbConnection connection = new SQLiteConnection(LoadConnectionString());
            connection.Query<PurchaseModel>("Delete from Purchases where Id = @Id", new { Id = id });
        }
    }
}