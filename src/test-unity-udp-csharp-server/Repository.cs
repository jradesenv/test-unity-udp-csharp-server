using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace test_unity_udp_csharp_server
{
    public class Repository
    {
        private static string _baseFolder = System.IO.Directory.GetCurrentDirectory();
        private static string _accountsFilePath = System.IO.Path.Combine(_baseFolder, "data_files", "game_accounts.json");        

        public static void SaveGameAccounts(List<GameAccount> accounts)
        {
            Task.Run(() => {
                Console.WriteLine("[" + DateTime.Now.ToString("dd/MM/yyyy") + "] Saving game accounts...");
                string json = JsonConvert.SerializeObject(accounts);
                System.IO.File.WriteAllText(_accountsFilePath, json);
            });
        }

        public static List<GameAccount> LoadGameAccounts()
        {
            if (System.IO.File.Exists(_accountsFilePath)) {
                string json = System.IO.File.ReadAllText(_accountsFilePath);
                return JsonConvert.DeserializeObject<List<GameAccount>>(json);
            } else
            {
                return new List<GameAccount>();
            }
        }
    }
}
