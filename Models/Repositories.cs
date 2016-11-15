using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

public interface HasId {
    int Id { get; set; }
}

public interface IRepository<T> where T: class, HasId { 
    T Create(T item);
    IEnumerable<T> Read();
    IEnumerable<T> Read(Func<DbSet<T>, IEnumerable<T>> fn);
    T Read(int id);
    bool Update(T item);
    T Delete(int id);
    IEnumerable<T> FromSql(string sql);
}

public class Repo<T> : IRepository<T> where T : class, HasId {

    protected DB db;
    protected IEnumerable<T> table;
    protected DbSet<T> dbtable;

    public Repo(DB db, string tableName, Func<DbSet<T>,IEnumerable<T>> includer){
        this.db = db;
        dbtable = GetTable(tableName);
        table = includer(dbtable);
    }

    // utilities for setup
    private DbSet<T> GetTable(string tableName){
        return (DbSet<T>)db.GetType().GetProperty(tableName).GetValue(db);
    }
    public static void Register(IServiceCollection services, string n) {
        Register(services, n, dbset => dbset);
    }
    public static void Register(IServiceCollection services, string n, Func<DbSet<T>,IEnumerable<T>> includer) {
        services.AddScoped<IRepository<T>>(provider => {
            var db = provider.GetRequiredService<DB>();
            return new Repo<T>(db, n, includer);
        });
    }
    
    // CRUD stuff
    public T Create(T item){
        dbtable.Add(item);
        db.SaveChanges();
        return table.FirstOrDefault(x => x.Id == item.Id);
    }

    public IEnumerable<T> Read(){
        return table.ToList();
    }
    
    public IEnumerable<T> Read(Func<DbSet<T>, IEnumerable<T>> fn){
        return fn(dbtable).ToList();
    }
    
    public T Read(int id){
        return table.FirstOrDefault(x => x.Id == id);
    }
    
    public bool Update(T item){
        T actual = table.FirstOrDefault(x => x.Id == item.Id);
        if(actual != null) {
            dbtable.Remove(actual);
            item.Id = actual.Id;
            dbtable.Add(item);
            db.SaveChanges();
            return true;
        }
        return false;
    }
    
    public T Delete(int id){
        T actual = table.FirstOrDefault(x => x.Id == id);
        if(actual != null) {
            dbtable.Remove(actual);
            db.SaveChanges();
            return actual;
        }
        return null;
    }

    // SQL
    public IEnumerable<T> FromSql(string sql) => dbtable.FromSql(sql);

}
