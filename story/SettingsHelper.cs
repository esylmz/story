using System.Data.SqlClient;

public static class SettingsHelper
{
    // Bu bir string olmalı! SqlConnection değil.
    private static string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=story;Integrated Security=True";

    public static void SetSetting(string key, string value)
    {
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string query = @"
                IF EXISTS (SELECT 1 FROM Settings WHERE SettingKey = @key)
                    UPDATE Settings SET SettingValue = @value WHERE SettingKey = @key
                ELSE
                    INSERT INTO Settings (SettingKey, SettingValue) VALUES (@key, @value)";
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@value", value);
            cmd.ExecuteNonQuery();
        }
    }

    public static string GetSetting(string key)
    {
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string query = "SELECT SettingValue FROM Settings WHERE SettingKey = @key";
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@key", key);
            var result = cmd.ExecuteScalar();
            return result?.ToString();
        }
    }
}
