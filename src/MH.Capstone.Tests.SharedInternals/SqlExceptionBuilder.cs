using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace MH.Capstone.Tests.SharedInternals
{
    // Adapted from https://stackoverflow.com/questions/11976996/moq-and-throwing-a-sqlexception
    public class SqlExceptionBuilder
    {
        private int errorNumber;
        private string? errorMessage;

        public SqlException Build()
        {
            SqlError error = CreateError();
            SqlErrorCollection errorCollection = CreateErrorCollection(error);
            SqlException exception = CreateException(errorCollection);

            return exception;
        }

        public SqlExceptionBuilder WithNumber(int number)
        {
            this.errorNumber = number;
            return this;
        }

        public SqlExceptionBuilder WithMessage(string message)
        {
            this.errorMessage = message;
            return this;
        }

        private SqlError CreateError()
        {
            // Create instance via reflection...
            var ctors = typeof(SqlError).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
            var firstSqlErrorCtor = ctors.FirstOrDefault(
                ctor =>
                    ctor.GetParameters().Count() == 8);
            SqlError error = firstSqlErrorCtor.Invoke(
                new object[]
                {
                    this.errorNumber,
                    new byte(),
                    new byte(),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    new int(),
                    new Exception()  // for .NetCore 
                }) as SqlError;

            return error;
        }

        private SqlErrorCollection CreateErrorCollection(SqlError error)
        {
            // Create instance via reflection...
            var sqlErrorCollectionCtor = typeof(SqlErrorCollection).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
            SqlErrorCollection errorCollection = sqlErrorCollectionCtor.Invoke(new object[] { }) as SqlErrorCollection;

            // Add error...
            typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(errorCollection, new object[] { error });

            return errorCollection;
        }

        private SqlException CreateException(SqlErrorCollection errorCollection)
        {
            // Create instance via reflection...
            var ctor = typeof(SqlException).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
            SqlException sqlException = ctor.Invoke(
                new object[]
                { 
                    // With message and error collection...
                    this.errorMessage,
                    errorCollection,
                    null,
                    Guid.NewGuid()
                }) as SqlException;

            return sqlException;
        }
    }
}
