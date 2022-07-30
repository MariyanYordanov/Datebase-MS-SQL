﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoMapper;
using Newtonsoft.Json;
using ProductShop.Data;
using ProductShop.DTOs.User;
using ProductShop.Models;
using System.ComponentModel.DataAnnotations;
using ProductShop.DTOs.Product;
using ProductShop.DTOs.Category;
using ProductShop.DTOs.CategoryProduct;
using AutoMapper.QueryableExtensions;

namespace ProductShop
{
    public class StartUp
    {
        private static string filePath;

        public static object ImportCategoryProductsDto { get; private set; }

        public static void Main(string[] args)
        {
            Mapper.Initialize(cfg => {
                cfg.AddProfile<ProductShopProfile>();
            });

            ProductShopContext dbContext = new ProductShopContext();
            InitializeOutputFilePath("users-and-products.json");

            //InitialaseDataSetFilePath("categories.json");
            //string inputJson = File.ReadAllText("categories-products.json");

            //dbContext.Database.EnsureDeleted();
            //dbContext.Database.EnsureCreated();

            //Console.WriteLine("Datababe copy was created!");

            //string output = ImportCategoryProducts(dbContext);
            //Console.WriteLine(output);

            string json = GetUsersWithProducts(dbContext);
            File.WriteAllText(filePath,json);
        }


        // Query 1.Import Users
        public static string ImportUsers(ProductShopContext context, string inputJson)
        {
            ImportUserDto[] userDtos = JsonConvert.DeserializeObject<ImportUserDto[]>(inputJson);

            ICollection<User> validUsers = new List<User>();

            foreach (ImportUserDto userDto in userDtos)
            {
                if (!IsAttributeValid(userDto))
                {
                    continue;
                }

                User user = Mapper.Map<User>(userDto);
                validUsers.Add(user);
            }

            context.Users.AddRange(validUsers);
            context.SaveChanges();

            return $"Successfully imported {validUsers.Count}";
        }

        // Query 2.Import Products
        public static string ImportProducts(ProductShopContext context, string inputJson)
        {
            ImportProductDto[] productDtos = JsonConvert.DeserializeObject<ImportProductDto[]>(inputJson);
            ICollection<Product> validProducts = new List<Product>();
            foreach (ImportProductDto productDto in productDtos)
            {
                if (!IsAttributeValid(productDto))
                {
                    continue;
                }

                Product product = Mapper.Map<Product>(productDto);
                validProducts.Add(product);
            }

            context.Products.AddRange(validProducts);
            context.SaveChanges();

            return $"Successfully imported {validProducts.Count}";
        }

        // Query 3.Import Categories
        public static string ImportCategories(ProductShopContext context, string inputJson)
        {
            ImportCategoryDto[] categoryDtos = JsonConvert.DeserializeObject<ImportCategoryDto[]>(inputJson);
            ICollection<Category> validCategories = new List<Category>();
            foreach (ImportCategoryDto categoryDto in categoryDtos)
            {
                if (!IsAttributeValid(categoryDto))
                {
                    continue;
                }

                Category category = Mapper.Map<Category>(categoryDto);
                validCategories.Add(category);
            }

            context.AddRange(validCategories);
            context.SaveChanges();

            return $"Successfully imported {validCategories.Count}";
        }

        // Query 4.Import Categories and Products
        public static string ImportCategoryProducts(ProductShopContext context, string inputJson)
        {
            ImportCategoryProductDto[] categoryProductsDtos = JsonConvert.DeserializeObject<ImportCategoryProductDto[]>(inputJson);
            ICollection<CategoryProduct> validCategoriesProducts = new List<CategoryProduct>();
            foreach (ImportCategoryProductDto categoryProductsDto in categoryProductsDtos)
            {
                if (!IsAttributeValid(categoryProductsDto))
                {
                    continue;
                }

                CategoryProduct categoryProduct = Mapper.Map<CategoryProduct>(categoryProductsDto);
                validCategoriesProducts.Add(categoryProduct);
            }

            context.AddRange(validCategoriesProducts);
            context.SaveChanges();

            return $"Successfully imported {validCategoriesProducts.Count}";
        }

        // Query 5. Export Products in Range
        public static string GetProductsInRange(ProductShopContext context)
        {
            ExportProductsInRangeDto[] products = context.Products
                .Where(p => p.Price >= 500 && p.Price <= 1000)
                .OrderBy(p => p.Price)
                .ProjectTo<ExportProductsInRangeDto>()
                .ToArray();

            string json = JsonConvert.SerializeObject(products, Formatting.Indented);

            return json;
        }

        // Query 6. Export Sold Products
        public static string GetSoldProducts(ProductShopContext context)
        {
            ExportUserWithSoldProductsDto[] users = context.Users
                .Where(u => u.ProductsSold.Any(p => p.BuyerId.HasValue))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ProjectTo<ExportUserWithSoldProductsDto>()
                .ToArray();

            string json = JsonConvert.SerializeObject(users, Formatting.Indented);

            return json;
        }

        // Query 7. Export Categories by Products Count
        public static string GetCategoriesByProductsCount(ProductShopContext context)
        {
            var categories = context.Categories
                .OrderByDescending(c => c.CategoryProducts.Count())
                .Select(c => new 
                {
                    category = c.Name,
                    productsCount = c.CategoryProducts.Count(),
                    averagePrice = c.CategoryProducts.Average(p => p.Product.Price).ToString("f2"),
                    totalRevenue = c.CategoryProducts.Sum(p => p.Product.Price).ToString("f2"),
                })
                .ToArray();

            string json = JsonConvert.SerializeObject(categories, Formatting.Indented);

            return json;
        }

        // Query 8. Export Users and Products
        public static string GetUsersWithProducts(ProductShopContext context)
        {
            /* Get all users who have at least 1 sold product with a buyer. 
             * Order them in descending order by the number of sold products with a buyer. 
             * Select only their first and last name, age and for each product - name and price. 
             * Ignore all null values.*/
            ExportUsersWithFullProductInfoDto[] users = context.Users
                .Where(u => u.ProductsSold.Any(p => p.BuyerId.HasValue))
                .OrderByDescending(u => u.ProductsSold.Count(p => p.BuyerId.HasValue))
                .ProjectTo<ExportUsersWithFullProductInfoDto>()
                .ToArray();

            ExportUsersInfoDto usersInfoDto = new ExportUsersInfoDto()
            {
                UsersCount = users.Length,
                Users = users,
            };

            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
            };

            string json = JsonConvert.SerializeObject(usersInfoDto, Formatting.Indented, jsonSerializerSettings);

            return json;
        }

        private static void InitialaseDataSetFilePath(string fileName)
        {
            filePath = Path.Combine(Directory.GetCurrentDirectory(), "../../../Datasets/", fileName);
        }

        private static void InitializeOutputFilePath(string fileName)
        {
            filePath = Path.Combine(Directory.GetCurrentDirectory(), "../../../Results/", fileName);
        }

        private static bool IsAttributeValid(object obj)
        {
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(obj);

            var validationResult = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(obj, validationContext, validationResult);

            return isValid;
        }
    }
}