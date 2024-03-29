﻿using Travel_App_Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Travel_App_Web.Data
{
    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Tour> Tours { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<EmailInfo> Emails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {            
            string userRoleName = "User";
            string adminRoleName = "Admin";

            Role userRole = new() { Id = 1, RoleName = userRoleName };
            Role adminRole = new() { Id = 2, RoleName = adminRoleName };

            modelBuilder.Entity<Role>().HasData(new Role[] { userRole, adminRole });

            modelBuilder.Entity<User>()
                .HasOne(user => user.Role)
                .WithMany();

            modelBuilder.Entity<User>()
                .HasMany(user => user.Chats)
                .WithMany();

            modelBuilder.Entity<Chat>()
                .HasMany(chat => chat.Messages)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Chat>()
                .HasMany(chat => chat.InterlocutorsEmails)
                .WithMany();

            modelBuilder.Entity<DayOfTour>()
                .HasMany(day => day.Images)
                .WithOne();

            modelBuilder.Entity<Tour>()
                .OwnsMany(tour => tour.Prices, apartment =>
                {
                    apartment.Property(apartment => apartment.ApartmentType).IsRequired();
                    apartment.Property(apartment => apartment.PriceEUR).HasColumnType("decimal(18, 2)").IsRequired();
                });

            base.OnModelCreating(modelBuilder);
        }
    }
}
