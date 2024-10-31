using FindJobs.DataAccess.Entities;
using FindJobs.DataAccess.Persistence;
using FindJobs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;


namespace FindJobs.DataAccess.Repositories
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        private readonly Test-1DbContext context;
        private DbSet<TEntity> DbSet { get; }

        public GenericRepository(FindJobsDbContext context)
        {
            this.context = context;
            this.DbSet = context.Set<TEntity>();
        }
        public IQueryable<TEntity> GetEntities()
        {
            return DbSet.AsQueryable();
        }


        public async Task AddEntity(TEntity entity)
        {
            Type myType = entity.GetType();
            IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());
            if (props.Any(x => x.Name == "LastUpdateDate"))
            {
                props.Where(x => x.Name == "LastUpdateDate").First().SetValue(entity, DateTime.Now);
                props.Where(x => x.Name == "CreateDate").First().SetValue(entity, DateTime.Now);
                props.Where(x => x.Name == "IsDeleted").First().SetValue(entity, false);
            }
            await DbSet.AddAsync(entity);
        }
        public async Task AddEntityRange(IEnumerable<TEntity> entities)
        {
            foreach (TEntity entity in entities)
            {
                Type myType = entity.GetType();
                IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());
                if (props.Any(x => x.Name == "LastUpdateDate"))
                {
                    props.Where(x => x.Name == "LastUpdateDate").First().SetValue(entity, DateTime.Now);
                    props.Where(x => x.Name == "CreateDate").First().SetValue(entity, DateTime.Now);
                    props.Where(x => x.Name == "IsDeleted").First().SetValue(entity, false);
                }
            }
            await DbSet.AddRangeAsync(entities);

        }
        public void RemoveEntity(TEntity entity)
        {
            Type myType = entity.GetType();
            IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());
            if (props.Any(x => x.Name == "IsDeleted"))
            {
                props.Where(x => x.Name == "IsDeleted").First().SetValue(entity, true);
            }
            UpdateEntity(entity);
        }
        public void DeleteEntity(TEntity entity)
        {
            DbSet.Remove(entity);
        }
        public void RemoveEntityRange(IEnumerable<TEntity> entities)
        {

            foreach (var entity in entities)
            {
                Type myType = entity.GetType();
                IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());
                if (props.Any(x => x.Name == "IsDeleted"))
                {
                    props.Where(x => x.Name == "IsDeleted").First().SetValue(entity, true);
                }
            }

            UpdateEntityRange(entities);
        }

        public void UpdateEntity(TEntity entity)
        {
            Type myType = entity.GetType();
            IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());
            if (props.Any(x => x.Name == "LastUpdateDate"))
            {
                props.Where(x => x.Name == "LastUpdateDate").First().SetValue(entity, DateTime.Now);
            }
            DbSet.Update(entity);
        }
        public void UpdateEntityRange(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                Type myType = entity.GetType();
                IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());
                if (props.Any(x => x.Name == "LastUpdateDate"))
                {
                    props.Where(x => x.Name == "LastUpdateDate").First().SetValue(entity, DateTime.Now);
                }
            }

            DbSet.UpdateRange(entities);
        }
        public async Task SaveChange()
        {
            await context.SaveChangesAsync();
        }
        public void Dispose()
        {
            context?.Dispose();
        }

        public void AddEntityRangeSeed(IEnumerable<TEntity> entities)
        {
            const int countPerInsert = 50000;
            for (int i = 0; i < entities.Count(); i += countPerInsert)
            {
                var tempEntities = entities.Skip(i).Take(countPerInsert);

                foreach (TEntity entity in tempEntities)
                {
                    Type myType = entity.GetType();
                    IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());
                    if (props.Any(x => x.Name == "LastUpdateDate"))
                    {
                        props.Where(x => x.Name == "LastUpdateDate").First().SetValue(entity, DateTime.Now);
                        props.Where(x => x.Name == "CreateDate").First().SetValue(entity, DateTime.Now);
                        props.Where(x => x.Name == "IsDeleted").First().SetValue(entity, false);
                    }
                }
                DbSet.AddRange(tempEntities);
                context.SaveChanges();
            }
        }
    }
}