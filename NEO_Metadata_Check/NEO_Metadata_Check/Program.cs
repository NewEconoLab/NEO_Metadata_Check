using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.IO;
using System.Collections.Generic;
using NEL.MongoHelper ;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Linq;

namespace NEO_Metadata_Check
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection()    //将配置文件的数据加载到内存中
                .SetBasePath(Directory.GetCurrentDirectory())   //指定配置文件所在的目录
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)  //指定加载的配置文件
                .Build();    //编译成对象

            string mongodbConnStr_data = config["mongodbConnStr_data"];
            string mongodbDatabase_data = config["mongodbDatabase_data"];
            string mongodbConnStr_target = config["mongodbConnStr_target"];
            string mongodbDatabase_target = config["mongodbDatabase_target"];

            NEL_MongoHelper nEL_MongoHelper_data = new NEL_MongoHelper(mongodbConnStr_data, mongodbDatabase_data);
            NEL_MongoHelper nEL_MongoHelper_target = new NEL_MongoHelper(mongodbConnStr_target, mongodbDatabase_target);

            Console.WriteLine("input blockHeight_target start:");
            Console.WriteLine("1 : check Blcok");
            Console.WriteLine("2 : check Tx");
            Int64 modeCode = Int64.Parse(Console.ReadLine());

            Console.WriteLine("input blockHeight_target start:");
            Int64 blockHeight_target_start = Int64.Parse(Console.ReadLine());

            Console.WriteLine("input blockHeight_target end:");
            Int64 blockHeight_target_end = Int64.Parse(Console.ReadLine());

            //初始化
            //nEL_MongoHelper_data.SetSystemCounter("block_check", -1);

            Console.WriteLine("start......");
            for (Int64 doBlockIndex= blockHeight_target_start; doBlockIndex <= blockHeight_target_end; doBlockIndex++)
            {
                JArray block = nEL_MongoHelper_target.GetData("block", "{index:" + doBlockIndex + "}");

                switch (modeCode) {
                    case 1:
                        if (block.Count != 1)
                        {
                            nEL_MongoHelper_data.InsertData("block_error", JObject.Parse("{index:" + doBlockIndex + "}"));
                        }
                        nEL_MongoHelper_data.SetSystemCounter("block_check", doBlockIndex);
                        break;
                    case 2:
                        JArray TXs_Source = (JArray)block[0]["tx"];
                        
                        List<string> TXIDs_Source = new List<string>();
                        foreach (JObject J in TXs_Source)
                        {
                            TXIDs_Source.Add((string)J["txid"]);
                        }

                        JArray TXs_Target = nEL_MongoHelper_target.GetData("tx", "{blockindex:" + doBlockIndex + "}");
                        List<string> TXIDs_Target = new List<string>();
                        foreach (JObject J in TXs_Target)
                        {
                            TXIDs_Target.Add((string)J["txid"]);
                        }

                        //少入的txid
                        var TXIDs_Except_Source_Target = TXIDs_Source.Except(TXIDs_Target);
                        if (TXIDs_Except_Source_Target.Count() > 0)
                        {
                            foreach (string txid in TXIDs_Except_Source_Target)
                            {
                                nEL_MongoHelper_data.InsertData("txid_error_less", JObject.Parse("{index:" + doBlockIndex + ",txid:'" + txid +"'}"));
                            }
                            
                        }

                        //多入的txid
                        var TXIDs_Except_Target_Source = TXIDs_Target.Except(TXIDs_Source);
                        if (TXIDs_Except_Target_Source.Count() > 0)
                        {
                            foreach (string txid in TXIDs_Except_Target_Source)
                            {
                                nEL_MongoHelper_data.InsertData("txid_error_many", JObject.Parse("{index:" + doBlockIndex + ",txid:'" + txid + "'}"));
                            }
                        }

                        nEL_MongoHelper_data.SetSystemCounter("tx_check", doBlockIndex);
                        break;
                    default:
                        break;
                }

                if ((doBlockIndex % 1000) == 0)
                {
                    Console.WriteLine(((decimal)(doBlockIndex - blockHeight_target_start) / (decimal)(blockHeight_target_end - blockHeight_target_start)).ToString("p4"));
                }

                //Thread.Sleep(5);
            }

        }
    }
}
