﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Models.Attributes;
using NpgsqlTypes;

namespace Models
{
    public class Product : IModel
    {
        [PrimaryKey]
        public int Id { get; set; }
        
        [SimpleProperty]
        public string Name { get; set; }
        
        [SimpleProperty]
        public int Cost { get; set; }
        
        [SimpleProperty]
        public string Description { get; set; }
        
        [NotAdministered]
        public int CategoryId { get; set; }
        
        [ManyToOne]
        public Category Category { get; set; }
        
        [ManyToMany]
        public IEnumerable<SpecificationOption> SpecificationOptions { get; set; }
        
        [OneToMany]
        public IEnumerable<Image> Images { get; set; }
        public IEnumerable<Purchase> Purchases { get; set; }
        
        [NotAdministered]
        public NpgsqlTsVector SearchVector { get; set; }
        
        [NotMapped]
        [NotAdministered]
        public bool IsInCart { get; set; }
        
        public override string ToString()
        {
            return $"{Id}.{Name}";
        }

        public static IModel GetModel(int id)
        {
            var context = new ApplicationContext();
            return context
                .Product
                .Include(product => product.Category)
                .Include(product => product.SpecificationOptions)
                .Include(product => product.Images)
                .First(product => product.Id == id);
        }

        public Product(string name, int cost, string description, int category, List<int> specificationOptions, List<int> images, ApplicationContext context = null)
        {
            Name = name;
            Cost = cost;
            Description = description;
            
            if (category > 0)
                Category = context?.Category.First(c => c.Id == category);
            
            if (specificationOptions.Any())
                SpecificationOptions = context?.SpecificationOption.Where(sOp => specificationOptions.Contains(sOp.Id));
            
            if (images.Any())
                Images = context?.Image.Where(i => images.Contains(i.Id));
        }

        public void Update(string name, int cost, string description, int category, List<int> specificationOptions, List<int> images, ApplicationContext context = null)
        {
            Name = name;
            Cost = cost;
            Description = description;
            
            if (category > 0)
                Category = context?.Category.First(c => c.Id == category);
            
            if (specificationOptions.Any())
                SpecificationOptions = context?.SpecificationOption.Where(sOp => specificationOptions.Contains(sOp.Id));
            
            if (images.Any())
                Images = context?.Image.Where(i => images.Contains(i.Id));
        }
        
        public Product()
        {
        }
    }
}