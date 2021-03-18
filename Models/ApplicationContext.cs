﻿using Microsoft.EntityFrameworkCore;

namespace Models
{
    public class ApplicationContext : DbContext
    {
        private const string HerokuConnectionString =
            "Host=ec2-176-34-222-188.eu-west-1.compute.amazonaws.com;Database=d3p2plhcg9prre;Username=hwuxdanoihasjy;Password=c6c4ce1fd9426a50359518d76885f62cb8da94049eeec4d89d00db60dbcd94a6;sslmode=Require;TrustServerCertificate=true";

        private const string LocalConnectionString =
            "Host=localhost;Database=ascension_db;Username=postgres;Password=qweasd123";
        
        public DbSet<Product> Product { get; set; }
        public DbSet<SpecificationOption> SpecificationOption { get; set; }
        public DbSet<Specification> Specification { get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<SuperCategory> SuperCategory { get; set; }
        public DbSet<Image> Image { get; set; }
        public DbSet<User> User { get; set; }


        public ApplicationContext()
        {
        }

        public ApplicationContext(DbContextOptions options)
            : base(options)
        {
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(HerokuConnectionString);
            }
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "Russian_Russia.1251");
        }
    }
}