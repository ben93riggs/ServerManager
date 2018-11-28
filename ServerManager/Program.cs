using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace ServerManager
{
    class Program
    {
        private const string Path = @"..\Logs\SERVER_MANAGER\SERVER_MANAGER.txt";

        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private SocketGuild _guild;

        private bool internal_status;
        private bool osu_status;
        private bool cloud_config_status;
        private bool internal_beta_status;

        string GetInternalStatus()
        {
            return (internal_status ? "Online" : "Offline");
        }
        string GetOsuStatus()
        {
            return (osu_status ? "Online" : "Offline");
        }
        string GetCloudConfigStatus()
        {
            return (cloud_config_status ? "Online" : "Offline");
        }
        string GetInternalBetaStatus()
        {
            return (internal_beta_status ? "Online" : "Offline");
        }

        string GetInternalStatus_emoji()
        {
            return (internal_status ? ":white_check_mark:" : ":x:");
        }
        string GetOsuStatus_emoji()
        {
            return (osu_status ? ":white_check_mark:" : ":x:");
        }
        string GetCloudConfigStatus_emoji()
        {
            return (cloud_config_status ? ":white_check_mark:" : ":x:");
        }

        private async Task MainAsync()
        {
            _client = new DiscordSocketClient();

            _client.Log += Log;
            _client.MessageReceived += MessageReceived;
            _client.Ready += ReadyAsync;

            const string token = "//removed_token//"; // Remember to keep this private!
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private async Task ReadyAsync()
        {
            foreach (var guild in _client.Guilds)
            {
                if (guild.Name.Equals("//removed//"))
                    _guild = guild;
            }

            await _client.SetGameAsync("//removed//");

            while (true)
            {
                internal_status = false;
                osu_status = false;
                cloud_config_status = false;
                internal_beta_status = false;

                var procs = Process.GetProcessesByName("//removed//");
                if (procs.Length > 0)
                {
                    foreach (var proc in procs)
                    {
                        string name = proc.MainWindowTitle;

                        if (name.EndsWith("OSU"))
                        {
                            osu_status = true;
                        }
                        else if (name.EndsWith("CONFIG"))
                        {
                            cloud_config_status = true;
                        }
                        else if (name.EndsWith("INTERNAL_BETA"))
                        {
                            internal_beta_status = true;
                        }
                        else if (name.EndsWith("INTERNAL"))
                        {
                            internal_status = true;
                        }
                    }
                }

                await Log(new LogMessage(LogSeverity.Info, "srvr_stat", "Updated server status."));
                await Log(new LogMessage(LogSeverity.Info, "srvr_stat", "Internal:\t" + GetInternalStatus()));
                await Log(new LogMessage(LogSeverity.Info, "srvr_stat", "Cloud Config:\t" + GetCloudConfigStatus()));
                await Log(new LogMessage(LogSeverity.Info, "srvr_stat", "OSU:\t" + GetOsuStatus()));
                await Log(new LogMessage(LogSeverity.Info, "srvr_stat", "Internal Beta:\t" + GetInternalBetaStatus()));

                var chan = _client.GetChannel(1234/*removed*/);
                if (chan is IMessageChannel textChannel)
                {
                    var msgs = textChannel.GetMessagesAsync(1).Flatten().Result;
                    await textChannel.DeleteMessagesAsync(msgs);
                    Thread.Sleep(100);
                    await textChannel.SendMessageAsync("CSGO_INTERNAL: " + GetInternalStatus_emoji() + " | CLOUD_CONFIG: " + GetCloudConfigStatus_emoji() + " | OSU: " + GetOsuStatus_emoji());
                    Thread.Sleep(1800000);
                }

                Thread.Sleep(3000);
            }
        }

        private Task Log(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case LogSeverity.Verbose:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }

            Console.WriteLine(msg.ToString());

            using (StreamWriter sw = File.AppendText(Path))
                sw.WriteLineAsync(msg.ToString());

            return Task.CompletedTask;
        }

        private Task Log(string source, string log)
        {
            LogMessage msg = new LogMessage(LogSeverity.Info, source, log);

            Console.WriteLine(msg.ToString());

            //write log file
            using (StreamWriter sw = File.AppendText(Path))
                sw.WriteLineAsync(msg.ToString());


            return Task.CompletedTask;
        }

        private T ReadXML<T>(string path)
        {
            try
            {
                object ret;
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    ret = new XmlSerializer(typeof(T)).Deserialize(file);
                    file.Close();
                }
                return (T)ret;
            }
            catch { return default(T); }
        }

        private void WriteXML(object obj, string path)
        {
            try
            {
                using (var file = new FileStream(path, FileMode.Create))
                {
                    new XmlSerializer(obj.GetType()).Serialize(file, obj);
                    file.Close();
                }

                return;
            }
            catch (Exception ex)
            {
                return;
            }
        }

        private async Task<string> GetNicknameFromFullName(string fullname)
        {
            try
            {
                string nickname = null;
                foreach (var usr in _guild.Users)
                {
                    if (usr.Username.Equals(fullname))
                    {
                        nickname = usr.Nickname;
                    }
                }

                if (String.IsNullOrEmpty(nickname))
                {
                    throw new Exception("No nickname found for the user: " + fullname);
                }

                return nickname;
            }
            catch (Exception e)
            {
                await Log(new LogMessage(LogSeverity.Warning, "HWID", e.Message));
                return "";
            }
        }

        private static async Task<string> GetAsync(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
                if (stream != null)
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return await reader.ReadToEndAsync();
                    }

            return "";
        }

        private JObject GetProfileInfo(string userid)
        {
            string html = string.Empty;
            string url = "//removed//";
            
            return JObject.Parse(GetAsync(url).Result);
        }

        private async Task MessageReceived(SocketMessage message)
        {
            if (message.Content == "!help")
            {
                await message.Author.SendMessageAsync("Commands: !help, !status, !hwidreset <username>");
            }
            else if (message.Content == "!status")
            {
                await message.Channel.SendMessageAsync("CSGO_INTERNAL: " + GetInternalStatus_emoji() +
                                                       " | CLOUD_CONFIG: " + GetCloudConfigStatus_emoji() + " | OSU: " +
                                                       GetOsuStatus_emoji());
            }
            else if (message.Content == "!hwidreset")
            {
                var author = message.Author;

                if (!File.Exists(string.Format("..\\Clients\\{0}\\{0}.xml", author.Username)))
                {
                    await author.SendMessageAsync(
                        "Username not found. Please make sure your nickname matches your forums username.");
                }
                else
                {
                    try
                    {
                        clsClient client =
                            ReadXML<clsClient>(string.Format("..\\Clients\\{0}\\{0}.xml", author.Username));

                        if (client == default(clsClient))
                            throw new Exception(author.Username + " FAILED to reset their HWID! - clsClient was null!");

                        string last_valid_ip = "";
                        bool foundIP = false;

                        foreach (var valid_ip in client.accessedFrom)
                        {
                            if (client.attemptedAccessFrom.Last().Contains(valid_ip))
                            {
                                foundIP = true;
                                await Log("HWID", valid_ip + " in " + client.attemptedAccessFrom.Last());
                            }

                            last_valid_ip = valid_ip;
                        }

                        if (last_valid_ip == "" || foundIP == false)
                            throw (new Exception(author.Username + " FAILED to reset their HWID!"));

                        client.bHWID = client.badHWID.Last();
                        client.hwidLocked = true;
                        WriteXML(client, string.Format("..\\Clients\\{0}\\{0}.xml", author.Username));
                        await Log(new LogMessage(LogSeverity.Warning, "HWID",
                            author.Username + " has SUCCESSFULLY reset their HWID!"));
                        await author.SendMessageAsync("Your HWID has been successfully updated! :white_check_mark:");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        await Log(new LogMessage(LogSeverity.Warning, "HWID", e.Message));
                        await author.SendMessageAsync(
                            ":x: Sorry, but an automatic reset cannot be performed for your account. Make sure that you have attempted to log in to the client before attempting to reset your HWID. Please submit a support ticket at https://removed.removed/support if this still fails and we will reset your HWID within 24 hours and extend your subscription for any time lost. Thank you for your patience. :heart:");
                        throw;
                    }
                }
            }
            else if (message.Content.StartsWith("!hwidreset "))
            {
                try
                {
                    int permission = 0, targetpermission = 3;

                    //get author permissions
                    var author = message.Author as SocketGuildUser;
                    var GuildUser = (IGuildUser)author;
                    if (GuildUser != null)
                    {
                        foreach (var role in GuildUser.Guild.Roles)
                        {
                            if (role.Name == "Moderator")
                            {
                                permission = 1;
                            }
                            else if (role.Name == "Community Administrator")
                            {
                                permission = 2;
                            }
                            else if (role.Name == "Developers")
                            {
                                permission = 3;
                            }
                        }
                    }

                    if (permission <= 1)
                    {
                        await Log(new LogMessage(LogSeverity.Info, "HWID",
                            message.Author.Username + " has invalid permissions to HWID reset user " +
                            message.Tags.First().Value));
                        return;
                    }

                    //get tagged user permissions
                    var tagged_user_string = message.Tags.First().Value.ToString();
                    var tagged_user = _client.GetUser(tagged_user_string.Split('#')[0],
                        tagged_user_string.Split('#')[1]);
                    var tagged_user_sgu = tagged_user as SocketGuildUser;
                    var tagged_user_gu = (IGuildUser)tagged_user_sgu;
                    if (tagged_user_gu != null)
                    {
                        foreach (var role in tagged_user_gu.Guild.Roles)
                        {
                            if (role.Name == "Moderator")
                            {
                                targetpermission = 1;
                            }
                            else if (role.Name == "Community Administrator")
                            {
                                targetpermission = 2;
                            }
                            else if (role.Name == "Developers")
                            {
                                throw new Exception("You cannot reset the HWID of a developer!");
                            }
                        }
                    }

                    if (targetpermission >= permission)
                        throw new Exception(
                            "You cannot reset the HWID of a user of equal or higher rank. Please contact support for assistance.");

                    if (message.Content.Length <= 11)
                        throw new Exception("Please specify a username. Example: !hwidreset hunter2");

                    var target_username = GetNicknameFromFullName(message.Tags.First().Value.ToString());

                    if (!File.Exists(string.Format("..\\Clients\\{0}\\{0}.xml", target_username)))
                        throw new Exception(
                            "Username not found. Please make sure the specified name matches the forums username.");

                    clsClient client = ReadXML<clsClient>(string.Format("..\\Clients\\{0}\\{0}.xml", target_username));

                    if (client == default(clsClient))
                        throw new Exception(target_username + " FAILED to reset their HWID! - clsClient was null!");

                    string last_valid_ip = "";
                    bool foundIP = false;

                    foreach (var valid_ip in client.accessedFrom)
                    {
                        if (client.attemptedAccessFrom.Last().Contains(valid_ip))
                        {
                            foundIP = true;
                            await Log("HWID", valid_ip + " in " + client.attemptedAccessFrom.Last());
                        }

                        last_valid_ip = valid_ip;
                    }

                    if (last_valid_ip == "" || foundIP == false)
                        throw (new Exception(target_username + " FAILED to reset their HWID!"));

                    client.bHWID = client.badHWID.Last();
                    client.hwidLocked = true;
                    WriteXML(client, string.Format("..\\Clients\\{0}\\{0}.xml", target_username));
                    await Log(new LogMessage(LogSeverity.Warning, "HWID",
                        target_username + " has SUCCESSFULLY reset their HWID!"));

                }
                catch (Exception e)
                {
                    await Log(new LogMessage(LogSeverity.Warning, "HWID", e.Message));
                    await message.Author.SendMessageAsync(
                        ":x: Sorry, but an automatic reset cannot be performed for your account. Make sure that you have attempted to log in to the client before attempting to reset your HWID. Please submit a support ticket at https://removed.removed/support if this still fails and we will reset your HWID within 24 hours and extend your subscription for any time lost. Thank you for your patience. :heart:");
                    throw;
                }
            }
            else if (message.Content.Equals("!rolerequest"))
            {
                await message.Author.SendMessageAsync("Invalid format, please use the command like so: !rolerequest <profile url>");
            }
            else if (message.Content.StartsWith("!rolerequest "))
            {
                var username = message.Author.Username;
                string userid = message.Content.Substring(message.Content.IndexOf("profile/", StringComparison.Ordinal) + "profile/".Length, 4);
                userid = userid.Split('-')[0];

                var result = GetProfileInfo(userid);

                string forumname = result["name"].ToString();
                string forum_discordname = result["customFields"]["2"]["fields"]["2"]["value"].ToString();
                string groupsPrimary = result["primaryGroup"]["id"].ToString();
                string groupsSecondary = result["secondaryGroups"].ToString();

                if (forumname != username || !forum_discordname.Contains(message.Author.Discriminator))
                {
                    await message.Author.SendMessageAsync("Invalid format. Make sure you have entered your discord username in the format John#1234 on your forums profile. Command usage example: !rolerequest <profile url>");
                    return;
                }



            }
            else if (message.Content.Equals("!staff"))
            {
                await message.Channel.SendMessageAsync("```css\n" +
                                                       "removed (owner/dev | removed, removed)\n" +
                                                       "removed (owner/dev | removed, removed, removed)\n" +
                                                       "removed (dev | removed)\n" +
                                                       "removed (dev | removed)\n" +
                                                       "removed (dev | removed)\n" +
                                                       "removed (community administrator)\n" +
                                                       "removed (moderator)\n" +
                                                       "removed (moderator)\n" +
                                                       "removed (moderator)" +
                                                       "```");
            }
            else if (message.Content.StartsWith("!warn "))
            {
                //get author permissions
                var author = message.Author as SocketGuildUser;
                var guildUser = (IGuildUser)author;
                var permission = 0;
                if (guildUser != null)
                {
                    foreach (var role in guildUser.Guild.Roles)
                    {
                        switch (role.Name)
                        {
                            case "Moderator":
                                permission = 1;
                                break;
                            case "Community Administrator":
                                permission = 2;
                                break;
                            case "Developers":
                                permission = 3;
                                break;
                            default:
                                return;
                        }
                    }
                }

                if (permission < 1)
                    return;

                if (message.Content.Length <= 6) { 
                    await Log("WARN", "Please tag the user. Example: !warn @hunter2#4567");
                    return;
                }

                var taggedUserString = message.Tags.First().Value.ToString();
                var taggedUser = _client.GetUser(taggedUserString.Split('#')[0], taggedUserString.Split('#')[1]);
                var tagged_user_sgu = taggedUser as SocketGuildUser;
                var tagged_user_gu = (IGuildUser)tagged_user_sgu;
                var userid_string = taggedUser.Id.ToString();
                await Log("WARN", taggedUser.Id.ToString());

                var path = "warnings\\" + userid_string + ".json";

                if (!File.Exists(path)) { 
                    File.Create(path);
                    File.WriteAllText(path, JsonConvert.SerializeObject(new UserWarnFile { userid = userid_string, warnings = 1 }, Formatting.Indented));
                    return;
                }
                else
                {
                    // read file into a string and deserialize JSON to a type
                    UserWarnFile warnFile = JsonConvert.DeserializeObject<UserWarnFile>(File.ReadAllText(path));

                    warnFile.warnings++;
                    if (warnFile.warnings >= 3)
                    {
                        tagged_user_gu.AddRoleAsync(role);
                        var user = Context.User;
                        var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == "RoleName");
                    }
                }

            }
        }
    }
}
