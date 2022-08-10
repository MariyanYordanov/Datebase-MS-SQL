﻿namespace TeisterMask.Data
{
    using Microsoft.EntityFrameworkCore;
    using System.ComponentModel.DataAnnotations;
    using TeisterMask.Data.Models;

    public class TeisterMaskContext : DbContext
    {
        public TeisterMaskContext() { }

        public TeisterMaskContext(DbContextOptions options)
            : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder
                    .UseSqlServer(Configuration.ConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<EmployeeTask>()
                .HasKey(et => new
                {
                    et.EmployeeId,
                    et.TaskId,
                });
        }

        public static bool ValidEmailDataAnnotations(string input)
        {
            return new EmailAddressAttribute().IsValid(input);
        }

        public DbSet<Employee> Employees { get; set; }

        public DbSet<EmployeeTask> EmployeesTasks { get; set; }

        public DbSet<Project> Projects { get; set; }

        public DbSet<Task> Tasks { get; set; }
    }
}