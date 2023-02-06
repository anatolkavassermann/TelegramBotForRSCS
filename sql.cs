using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Org.BouncyCastle.Crypto;
using Telegram.Bot.Types;
using System.Collections;

namespace tb_lab
{
    internal class sql
    {
        public static string GetStudentName(string connectionString, long StudentTID)
        {
            string result = "";
            try
            {
                SqlConnection connection = new(connectionString);
                connection.Open();
                SqlCommand sqlCommand = new($"Select StudentName from Students.dbo.Student_Data where StudentTID=@StudentTID", connection);
                sqlCommand.Parameters.AddWithValue("StudentTID", StudentTID);
                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = (string)reader["StudentName"];
                    }
                }
                connection.Close();
            }
            catch
            {
                //Handle error
            }
            return result; //Not registered
        }
        public static bool VerifyStudent(string connectionString, long StudentTID, string nonce)
        {
            bool result = false;
            try
            {
                SqlConnection connection = new(connectionString);
                connection.Open();
                SqlCommand sqlCommand = new($"Select nonce from Students.dbo.Student_Data where StudentTID=@StudentTID", connection);
                sqlCommand.Parameters.AddWithValue("StudentTID", StudentTID);
                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if ((string)reader["nonce"] == nonce)
                        {
                            result = true;
                        }
                        else
                        {
                            result = false;
                        }

                    }
                }
                connection.Close();
            }
            catch
            {
                //Handle error
            }
            if (result)
            {
                try
                {
                    SqlConnection connection = new(connectionString);
                    connection.Open();
                    SqlCommand updateSqlCommand = new($"UPDATE Students.dbo.Student_Data SET IsVerified=@IsVerified where StudentTID=@StudentTID", connection);
                    updateSqlCommand.Parameters.AddWithValue("StudentTID", StudentTID);
                    updateSqlCommand.Parameters.AddWithValue("IsVerified", 1);
                    updateSqlCommand.ExecuteReader().CloseAsync();
                    connection.Close();
                }
                catch
                {
                    //Handle error
                }

            }
            return result; //Not registered
        }

        public static Hashtable GetStudentActiveTask(string connectionString, long StudentTID)
        {
            Hashtable tempResult = new();
            try
            {
                SqlConnection connection = new(connectionString);
                connection.Open();
                SqlCommand sqlCommand = new($"Select sendCertReqToCA, sendCertReqToAdmin, getResource from Students.dbo.Student_Data where StudentTID=@StudentTID", connection);
                sqlCommand.Parameters.AddWithValue("StudentTID", StudentTID);
                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            tempResult.Add(reader.GetName(i), reader.GetValue(i));
                        }
                    }
                }
                connection.Close();
            }
            catch
            {
                //Handle error
            }
            Hashtable result = new();
            foreach (System.Collections.DictionaryEntry task in tempResult)
            {
                if ((int)task.Value! > 0)
                    result.Add(task.Key, task.Value);
            }
            return result; //Not registered
        }

        public static void CancelAllTasks(string connectionString, long StudentTID)
        {
            try
            {
                SqlConnection connection = new(connectionString);
                connection.Open();
                SqlCommand sqlCommand = new($"UPDATE Students.dbo.Student_Data SET sendCertReqToCA=0, sendCertReqToAdmin=0, getResource=0 where StudentTID=@StudentTID", connection);
                sqlCommand.Parameters.AddWithValue("StudentTID", StudentTID);
                sqlCommand.ExecuteReader().CloseAsync();
                connection.Close();
            }
            catch
            {
                //Handle error
            }
            return;
        }

        public static void SetActiveTask(string connectionString, long StudentTID, string TaskName, int TaskStatus)
        {
            try
            {
                SqlConnection connection = new(connectionString);
                connection.Open();
                SqlCommand sqlCommand = new($"UPDATE Students.dbo.Student_Data SET {TaskName}=@TaskStatus where StudentTID=@StudentTID", connection);
                sqlCommand.Parameters.AddWithValue("TaskName", TaskName);
                sqlCommand.Parameters.AddWithValue("TaskStatus", TaskStatus);
                sqlCommand.Parameters.AddWithValue("StudentTID", StudentTID);
                sqlCommand.ExecuteReader().CloseAsync();
                connection.Close();
            }
            catch
            {
                //Handle error
            }
            return;
        }

        public static string GetStoredCertReq(string connectionString, long StudentTID)
        {
            string result = "";
            try
            {
                SqlConnection connection = new(connectionString);
                connection.Open();
                SqlCommand sqlCommand = new($"Select certReq from Students.dbo.Student_Data where StudentTID=@StudentTID", connection);
                sqlCommand.Parameters.AddWithValue("StudentTID", StudentTID);
                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = (string)reader["certReq"];
                    }
                }
                connection.Close();
            }
            catch
            {
                //Handle error
            }
            return result;

        }

        public static void StoreCertReq(string connectionString, long StudentTID, string certReq)
        {
            try
            {
                SqlConnection connection = new(connectionString);
                connection.Open();
                SqlCommand sqlCommand = null!;
                if (certReq == "")
                    sqlCommand = new($"UPDATE Students.dbo.Student_Data SET certReq=NULL where StudentTID=@StudentTID", connection);
                else
                    sqlCommand = new($"UPDATE Students.dbo.Student_Data SET certReq=@certReq where StudentTID=@StudentTID", connection);
                sqlCommand.Parameters.AddWithValue("certReq", certReq);
                sqlCommand.Parameters.AddWithValue("StudentTID", StudentTID);
                sqlCommand.ExecuteReader().CloseAsync();
                connection.Close();
            }
            catch
            {
                //Handle error
            }
            return;
        }

        public static string GetFlag(string connectionString, long StudentTID)
        {
            string result = "";
            try
            {
                SqlConnection connection = new(connectionString);
                connection.Open();
                SqlCommand sqlCommand = new($"Select Flag from Students.dbo.Student_Data where StudentTID=@StudentTID", connection);
                sqlCommand.Parameters.AddWithValue("StudentTID", StudentTID);
                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = (string)reader["Flag"];
                    }
                }
                connection.Close();
            }
            catch
            {
                //Handle error
            }
            return result;
        }

        public static bool IsStudentTIDALreadyBinded(string connectionString, string StudentID)
        {
            bool result = false;
            try
            {
                SqlConnection connection = new(connectionString);
                connection.Open();
                SqlCommand sqlCommand = new($"Select StudentTID from Students.dbo.Student_Data where StudentID=@StudentID", connection);
                sqlCommand.Parameters.AddWithValue("StudentID", StudentID);
                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (reader["StudentTID"] != System.DBNull.Value)
                        {
                            result = true;
                        }
                        else
                        {
                            result = false;
                        }

                    }
                }
                connection.Close();
            }
            catch
            {
                //Handle error
            }
            return result; //Not registered
        }

        static public string IsVerified(string connectionString, long StudentTID)
        {
            string result = "";
            try
            {
                SqlConnection connection = new(connectionString);
                connection.Open();
                SqlCommand sqlCommand = new($"Select IsVerified from Students.dbo.Student_Data where StudentTID=@StudentTID", connection);
                sqlCommand.Parameters.AddWithValue("StudentTID", StudentTID);

                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if ((bool)reader["IsVerified"])
                        {
                            result = "OK";
                        }
                        else
                        {
                            result = "NV"; //Not verified
                        }

                    }
                    else
                    {
                        result = "NR"; //Not registered
                    }
                }
                connection.Close();
            }
            catch
            {
                //Handle error
            }

            return result; //Not registered
        }

        static public void SetTID(string connectionString, long StudentTID, string StudentID)
        {
            try
            {
                SqlConnection connection = new(connectionString);
                connection.Open();
                SqlCommand sqlCommand = new($"UPDATE Students.dbo.Student_Data SET StudentTID=@StudentTID where StudentID=@StudentID", connection);
                sqlCommand.Parameters.AddWithValue("StudentID", StudentID);
                sqlCommand.Parameters.AddWithValue("StudentTID", StudentTID);
                sqlCommand.ExecuteReader().CloseAsync();
                connection.Close();
            }
            catch
            {
                //Handle error
            }
            return;
        }

        static public void UnsetTID(string connectionString, string StudentID)
        {
            try
            {
                SqlConnection connection = new(connectionString);
                connection.Open();
                SqlCommand sqlCommand = new($"UPDATE Students.dbo.Student_Data SET StudentTID=NULL where StudentID=@StudentID", connection);
                sqlCommand.Parameters.AddWithValue("StudentID", StudentID);
                sqlCommand.ExecuteReader().CloseAsync();
                connection.Close();
            }
            catch
            {
                //Handle error
            }
            return;
        }

        static public string GetStudentEmail(string connectionString, long StudentTID)
        {
            string studentEmail = "";
            try
            {
                SqlConnection connection = new(connectionString);
                connection.Open();
                SqlCommand sqlCommand = new($"Select StudentEmail from Students.dbo.Student_Data where StudentTID=@StudentTID", connection);
                sqlCommand.Parameters.AddWithValue("StudentTID", StudentTID);
                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        studentEmail = (string)reader["StudentEmail"];
                    }
                }
                connection.Close();
            }
            catch
            {
                //Handle error
            }
            return studentEmail;
        }

        public static bool DoesStudentExist(string connectionString, string StudentID)
        {
            bool result = false;
            try
            {
                SqlConnection connection = new(connectionString);
                connection.Open();
                SqlCommand sqlCommand = new($"Select * from Students.dbo.Student_Data where StudentID=@StudentID", connection);
                sqlCommand.Parameters.AddWithValue("StudentID", StudentID);

                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = true;

                    }
                    else
                    {
                        result = false;
                    }
                }
                connection.Close();
            }
            catch
            {
                //Handle error
            }

            return result; //Not registered
        }

        public static void SetNonce(string connectionString, long StudentTID, string nonce)
        {
            try
            {
                SqlConnection connection = new(connectionString);
                connection.Open();
                SqlCommand sqlCommand = new($"UPDATE Students.dbo.Student_Data SET Nonce=@Nonce where StudentTID=@StudentTID", connection);
                sqlCommand.Parameters.AddWithValue("Nonce", nonce);
                sqlCommand.Parameters.AddWithValue("StudentTID", StudentTID);
                sqlCommand.ExecuteReader().CloseAsync();
                connection.Close();
            }
            catch
            {
                //Handle error
            }
            return;
        }

        public static void CancelRegistration(string connectionString, long StudentTID)
        {
            try
            {
                SqlConnection connection = new(connectionString);
                connection.Open();
                SqlCommand sqlCommand = new($"UPDATE Students.dbo.Student_Data SET Nonce=NULL, StudentTID=NULL where StudentTID=@StudentTID", connection);
                sqlCommand.Parameters.AddWithValue("StudentTID", StudentTID);
                sqlCommand.ExecuteReader().CloseAsync();
                connection.Close();
            }
            catch
            {
                //Handle error
            }
            return;
        }
    }
}