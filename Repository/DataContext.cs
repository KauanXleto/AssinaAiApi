using AssinaAi.BusinessEntities;
using Microsoft.EntityFrameworkCore;
using System;

namespace AssinaAiApi.Repository
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        public DbSet<Person> Person { get; set; }
        public DbSet<UserInfo> UserInfo { get; set; }
        public DbSet<Simplification> Simplification { get; set; }
        public DbSet<SimplificationPoints> SimplificationPoints { get; set; }
        public DbSet<Archive> Archive { get; set; }
    }
}