﻿using Npgsql.FrontendMessages;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Npgsql
{
    /// <summary>
    /// Large object manager. This class can be used to store very large files in a PostgreSQL database.
    /// </summary>
    public class NpgsqlLargeObjectManager
    {
        const int INV_WRITE = 0x00020000;
        const int INV_READ = 0x00040000;

        internal readonly NpgsqlConnection _connection;

        /// <summary>
        /// The largest chunk size (in bytes) read and write operations will read/write each roundtrip to the network. Default 4 MB.
        /// </summary>
        public int MaxTransferBlockSize { get; set; }

        /// <summary>
        /// Creates an NpgsqlLargeObjectManager for this connection. The connection must be opened to perform remote operations.
        /// </summary>
        /// <param name="connection"></param>
        public NpgsqlLargeObjectManager(NpgsqlConnection connection)
        {
            _connection = connection;
            MaxTransferBlockSize = 4 * 1024 * 1024; // 4MB
        }

        /// <summary>
        /// Execute a function
        /// </summary>
        /// <param name="function"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        [GenerateAsync]
        internal T ExecuteFunction<T>(string function, params object[] arguments)
        {
            using (var command = new NpgsqlCommand(function, _connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = function;
                foreach (var argument in arguments)
                {
                    command.Parameters.Add(new NpgsqlParameter() { Value = argument });
                }
                return (T)command.ExecuteScalar();
            }
        }

        /// <summary>
        /// Execute a function that returns a byte array
        /// </summary>
        /// <param name="function"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        [GenerateAsync]
        internal int ExecuteFunctionGetBytes(string function, byte[] buffer, int offset, int len, params object[] arguments)
        {
            using (var command = new NpgsqlCommand(function, _connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                foreach (var argument in arguments)
                {
                    command.Parameters.Add(new NpgsqlParameter() { Value = argument });
                }
                using (var reader = command.ExecuteReader(System.Data.CommandBehavior.SequentialAccess))
                {
                    reader.Read();
                    return (int)reader.GetBytes(0, 0, buffer, offset, len);
                }
            }
        }

        /// <summary>
        /// Create an empty large object in the database. If an oid is specified but is already in use, an NpgsqlException will be thrown.
        /// </summary>
        /// <param name="preferredOid">A preferred oid, or specify 0 if one should be automatically assigned</param>
        /// <returns>The oid for the large object created</returns>
        /// <exception cref="NpgsqlException">If an oid is already in use</exception>
        [GenerateAsync]
        [CLSCompliant(false)]
        public uint Create(uint preferredOid = 0)
        {

            return ExecuteFunction<uint>("lo_create", (int)preferredOid);
        }

        /// <summary>
        /// Opens a large object on the backend, returning a stream controlling this remote object.
        /// A transaction snapshot is taken by the backend when the object is opened with only read permissions.
        /// When reading from this object, the contents reflects the time when the snapshot was taken.
        /// Note that this method, as well as operations on the stream must be wrapped inside a transaction.
        /// </summary>
        /// <param name="oid">Oid of the object</param>
        /// <returns>An NpgsqlLargeObjectStream</returns>
        [GenerateAsync]
        public NpgsqlLargeObjectStream OpenRead(uint oid)
        {
            var fd = ExecuteFunction<int>("lo_open", (int)oid, INV_READ);
            return new NpgsqlLargeObjectStream(this, oid, fd, false);
        }

        /// <summary>
        /// Opens a large object on the backend, returning a stream controlling this remote object.
        /// Note that this method, as well as operations on the stream must be wrapped inside a transaction.
        /// </summary>
        /// <param name="oid">Oid of the object</param>
        /// <returns>An NpgsqlLargeObjectStream</returns>
        [GenerateAsync]
        public NpgsqlLargeObjectStream OpenReadWrite(uint oid)
        {
            var fd = ExecuteFunction<int>("lo_open", (int)oid, INV_READ | INV_WRITE);
            return new NpgsqlLargeObjectStream(this, oid, fd, true);
        }

        /// <summary>
        /// Deletes a large object on the backend.
        /// </summary>
        /// <param name="oid">Oid of the object to delete</param>
        [GenerateAsync]
        public void Unlink(uint oid)
        {
            ExecuteFunction<object>("lo_unlink", (int)oid);
        }

        /// <summary>
        /// Exports a large object stored in the database to a file on the backend. This requires superuser permissions.
        /// </summary>
        /// <param name="oid">Oid of the object to export</param>
        /// <param name="path">Path to write the file on the backend</param>
        [GenerateAsync]
        public void ExportRemote(uint oid, string path)
        {
            ExecuteFunction<object>("lo_export", (int)oid, path);
        }

        /// <summary>
        /// Imports a large object to be stored as a large object in the database from a file stored on the backend. This requires superuser permissions.
        /// </summary>
        /// <param name="path">Path to read the file on the backend</param>
        /// <param name="oid">A preferred oid, or specify 0 if one should be automatically assigned</param>
        [GenerateAsync]
        public void ImportRemote(string path, uint oid = 0)
        {
            ExecuteFunction<object>("lo_import", path, (int)oid);
        }

        /// <summary>
        /// Since PostgreSQL 9.3, large objects larger than 2GB can be handled, up to 4TB.
        /// This property returns true whether the PostgreSQL version is >= 9.3.
        /// </summary>
        public bool Has64BitSupport { get { return _connection.PostgreSqlVersion >= new Version(9, 3); } }

        /*
        internal enum Function : uint
        {
            lo_open = 952,
            lo_close = 953,
            loread = 954,
            lowrite = 955,
            lo_lseek = 956,
            lo_lseek64 = 3170, // Since PostgreSQL 9.3
            lo_creat = 957,
            lo_create = 715,
            lo_tell = 958,
            lo_tell64 = 3171, // Since PostgreSQL 9.3
            lo_truncate = 1004,
            lo_truncate64 = 3172, // Since PostgreSQL 9.3

            // These four are available since PostgreSQL 9.4
            lo_from_bytea = 3457,
            lo_get = 3458,
            lo_get_fragment = 3459,
            lo_put = 3460,

            lo_unlink = 964,

            lo_import = 764,
            lo_import_with_oid = 767,
            lo_export = 765,
        }
        */
    }
}