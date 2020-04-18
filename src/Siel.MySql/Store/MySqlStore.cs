using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using Siel.Common;
using Siel.Scheduler.Event;
using Siel.Store;

namespace Siel.MySql.Store
{
    public class MySqlStore : ITaskStore, IEventStore
    {
        private readonly string _connectionString;

        private static string Sql = $@"
create table if not exists siel_task
(
    id                  varchar(36)      not null primary key,
    name                varchar(255)  not null,
    type_name           varchar(500)  not null,
    data                varchar(1000) null,
    properties          varchar(1000) null,
    success             int           not null default 0,
    failure             int           not null default 0,
    creation_time       timestamp   not null default current_timestamp   
);

create index IX_siel_task_name on siel_task (name);
create index IX_siel_task_creation_time on siel_task (creation_time);

create table if not exists siel_task_success
(
    id                bigint        not null auto_increment primary key,
    task_id           varchar(36)      not null,   
    creation_time     timestamp   not null default current_timestamp
);

create index IX_siel_task_success_creation_time on siel_task_success (creation_time);
create index IX_siel_task_success_task_id on siel_task_success (task_id);

create table if not exists siel_task_failure
(
    id                bigint        not null auto_increment primary key,
    task_id           varchar(36)      not null,
    stack_trace       text          null,
    creation_time     timestamp   not null default current_timestamp
);

create index IX_siel_task_failure_creation_time on siel_task_failure (creation_time);
create index IX_siel_task_failure_task_id on siel_task_failure (task_id);
";

        public MySqlStore(string connectionString)
        {
            _connectionString = connectionString;
            var connectionStringBuilder = new MySqlConnectionStringBuilder(_connectionString);
            var database = connectionStringBuilder.Database;
            connectionStringBuilder.Database = "mysql";
            using var conn = new MySqlConnection(connectionStringBuilder.ToString());
            conn.Execute($"CREATE SCHEMA IF NOT EXISTS {database} DEFAULT CHARACTER SET utf8mb4;");
            conn.Open();
            conn.ChangeDatabase(database);
            if (!conn.Query<string>("SHOW TABLES LIKE 'siel_task';").Any())
            {
                conn.Execute(Sql);
            }
        }

        public async ValueTask<bool> SaveAsync(PersistedTask task)
        {
            await using var conn = new MySqlConnection(_connectionString);
            return await conn.ExecuteAsync(
                $"insert ignore into siel_task  (id, name, type_name, data, properties) values (@Id, @Name, @TypeName, @Data, @Properties);",
                task) == 1;
        }

        public async Task<IEnumerable<PersistedTask>> TakeAsync(int page, int limit)
        {
            if (page <= 0)
            {
                throw new ArgumentException("Page should larger than 1");
            }

            if (limit <= 0)
            {
                throw new ArgumentException("Limit should larger than 1");
            }

            var start = (page - 1) * limit;
            await using var conn = new MySqlConnection(_connectionString);
            return await conn.QueryAsync<PersistedTask>(
                $"select id, name, type_name as TypeName, data, properties, success, failure, creation_time  from siel_task order by creation_time limit @Start, @Limit",
                new
                {
                    Start = start,
                    Limit = limit
                });
        }

        public async ValueTask<bool> RemoveAsync(string id)
        {
            await using var conn = new MySqlConnection(_connectionString);
            return await conn.ExecuteAsync(
                $"delete from siel_task_failure where task_id = @Id; delete from siel_task_success where task_id = @Id; delete from siel_task where id = @Id;",
                new
                {
                    Id = id
                }) > 0;
        }

        public async Task<PagedResult<PersistedTask>> PagedQueryAsync(string keyword, int page, int limit)
        {
            if (page <= 0)
            {
                throw new ArgumentException("Page should larger than 1");
            }

            if (limit <= 0)
            {
                throw new ArgumentException("Limit should larger than 1");
            }

            var result = new PagedResult<PersistedTask> {Page = page, Limit = limit};
            var start = (page - 1) * limit;
            await using var conn = new MySqlConnection(_connectionString);
            if (string.IsNullOrWhiteSpace(keyword))
            {
                var count = await conn.QuerySingleAsync<int>($"select count(*) from siel_task");
                result.Count = count;

                result.Data = (await conn.QueryAsync<PersistedTask>(
                    $"select * from siel_task order by creation_time limit @Start, @Limit", new
                    {
                        Start = start,
                        Limit = limit
                    })).ToList();
                return result;
            }
            else
            {
                var count = await conn.QuerySingleAsync<int>($"select count(*) from siel_task where name like @Like",
                    new
                    {
                        Like = $"{keyword}%"
                    });
                result.Count = count;

                result.Data = (await conn.QueryAsync<PersistedTask>(
                    $"select * from siel_task where name like @Like order by creation_time limit @Start, @Limit", new
                    {
                        Like = $"{keyword}%",
                        Start = start,
                        Limit = limit
                    })).ToList();
                return result;
            }
        }

        public async ValueTask<bool> UpdateAsync(PersistedTask task)
        {
            await using var conn = new MySqlConnection(_connectionString);
            return await conn.ExecuteAsync(
                $"update siel_task  set name = @Name, type_name=@TypeName, data = @Data, properties = @Properties where id = @Id",
                task) == 1;
        }

        public async Task FailAsync(FailureEvent @event)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.ExecuteAsync(
                $"insert into siel_task_failure (task_id, stack_trace) values (@Id, @StackTrace); update siel_task set failure = failure + 1 where id = @Id;",
                @event);
        }

        public async Task SuccessAsync(SuccessEvent @event)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.ExecuteAsync(
                $"insert into siel_task_success (task_id) values (@Id); update siel_task set success = success + 1 where id = @Id;",
                @event);
        }
    }
}