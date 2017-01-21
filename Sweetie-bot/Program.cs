using System;
using System.Collections.Generic;
using System.Linq;



namespace Sweetie_bot
{

    using Discord;
    using Discord.Audio;
    using Discord.Commands;
    using Newtonsoft.Json;
    using System.IO;
    using System.Timers;
    

    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }

    public class ProfanityCounter
    {
        private Timer profanityTimer;
        bool canReport;
        int count;
        bool profanityDeletion;
        int profanityDeletionHours;
        int profanityDeletionHoursCap;
         
        public bool Report
        {
            get { return canReport; }
        }

        public bool DeleteProfanity
        {
            get { return profanityDeletion; }
        }

        public ProfanityCounter()
        {
            profanityDeletionHours = 0;
            profanityDeletionHoursCap = 0;
            count = 0;
            canReport = false;
            profanityDeletion = false;
            profanityTimer = new Timer(60 * 60000);
            profanityTimer.Elapsed += sub;
            profanityTimer.AutoReset = true;
            profanityTimer.Start();
        }

        ~ProfanityCounter()
        {
            profanityTimer.Stop();
            profanityTimer.Dispose();
            profanityTimer = null;
        }

        public int add()
        {
            count++;

            if (count > 5) profanityDeletion = true;

            if (count == 6)
            {
                profanityDeletionHoursCap += 24;
                if (profanityDeletionHoursCap >= 24 * 7)
                    profanityDeletionHoursCap = 24 * 7;
                canReport = true;
            }
            else canReport = false;
            
            return Math.Min(count, 6);
        }

        private void sub(Object source, ElapsedEventArgs e)
        {
            if (profanityDeletion)
            {
                if (profanityDeletionHours >= profanityDeletionHoursCap)
                {
                    profanityDeletionHours = 0;
                    profanityDeletion = false;
                    count = 0;
                }
                else profanityDeletionHours++;
            }
            else
            {
                count = 0;
                profanityDeletionHoursCap -= 2;
                if (profanityDeletionHoursCap < 0)
                    profanityDeletionHoursCap = 0;
            }
        }
    }

    class Program
    {
        const string BOT_TOKEN_FILE = "./discord_token.txt";

        static void Main(string[] args) => new Program().Start();

        static int pokeState = 0;

        static bool filterEnabled = true;

        private void resetpokeState(Object source, ElapsedEventArgs e)
        {
            pokeState = 0;
        }

        private void endTimeout(object sender, ElapsedEventArgs e, CommandEventArgs ev, ulong userID, Role banrole)
        {
            Console.Write(string.Format("USER TIMEOUT EXPIRE: {0}", ev.Server.GetUser(userID).ToString()));
            ev.Server.GetUser(userID).RemoveRoles(banrole);
        }

        private static Timer pokeTimer = new Timer(60 * 1000);

        private DiscordClient _client = null;

        private CensorshipManager censorshipManager;

        List<string> meResponsesClean = new List<string>()
        {
            "gasps, \"Hey!\"",
            "shrinks away from {0}.",
            "tilts head, staring at {0} curiously.",
            "goes to {0}, nuzzling their leg.",
            "smiles happily.",
            "giggles to herself.",
            "falls asleep on {0}."
        };

        List<string> meResponsesLewd = new List<string>()
        {
            "stares at {0} with lidded eyes.",
            "turns away from {0}, presenting to them.",
            "rubs flank against {0}'s leg.",
            "bites lip seductively."
        };

        List<string> magic8phrases = new List<string>()
        {
            "You bet your cutiemark!",
            "Of course!",
            "No doubt!",
            "Yes, totally.",
            "I promise it's a yes.",
            "As far as I can tell, yes!",
            "Most likely.",
            "Looks like it!",
            "Yes",
            "Signs point to yes.",
            "Ask again later." ,
            "*falls asleep*",
            "It's better you don't know.",
            "I can't tell...",
            "How am I supposed to know?",
            "Don't count on it",
            "Nah.",
            "Nope",
            "No.",
            "I don't think so..."
        };

        List<string> magic8nsfw = new List<string>()
        {
            "If I say yes, will you fuck me already?",
            "Asking that is like asking if I'm horny, YES!",
            "Thats as likely as getting me on Big Mac.",
            "Uhh... no. Fucking, of course not."
        };

        List<string> nsfwChannels = new List<string>(new string[] {
            "general-nsfw",
            "non-mlp-young",
            "filly-only",
            "filly-x-colt",
            "colt-x-only",
            "filly-x-male",
            "filly-x-female",
            "colt-x-male",
            "colt-x-female",
            "mlp-loli",
            "mlp-shota",
            "roleplay",
            "mlp-loli-x-shota",
            "extreme-fetish-general",
            "extreme-fetish-loli-n-shota"
        });

        private static char PonyRolePrefix = '^';

        Dictionary<string, string> ponyRoles = new Dictionary<string, string>();

        Dictionary<string, ProfanityCounter> userProfanityCount = new Dictionary<string, ProfanityCounter>();

        List<string> profaneMessageResponses = new List<string>(new string[]
        {
            "Your message contained profanity, please replace the underlined content or post your message in a NSFW channel.",
            "I asked you nicely before, would you please replace the profane content of your message?",
            "Pretty please with a cherry on top?",
            "I'll ask again... PLEASE replace the profane content in your message!",
            "If you continue to post profane messages, future messages that contain profanity will be automatically deleted.",
            "You're making fun of me, aren't you?",
            "You know what... I showed that last message of yours to a certain somepony, and do you want to know what you've done?\rYou made Fluttershy cry!"
        });

        int GetProfanityCount(string name)
        {
            int count = 0;
            if (!userProfanityCount.ContainsKey(name))
                userProfanityCount.Add(name, new ProfanityCounter());
            else
                count = userProfanityCount[name].add();
            return count;
        }

        string GetOutputMessage(string name, int profanityCount, string message, string filtered)
        {
            string outputMessage;
            if (profanityCount < 3) outputMessage = censorshipManager.censor.Underline(message, filtered);
            else if (profanityCount < 6) outputMessage = censorshipManager.censor.BoldUnderline(message, filtered);
            else
            {
                outputMessage = censorshipManager.censor.FlutterCryFilter(message, filtered);
            }
            return outputMessage;
        }

        bool HasPonyRolePrerequisites(CommandEventArgs e)
        {
            Role[] everypone = e.Server.FindRoles("@everypone").ToArray();
            Role[] fillyscout = e.Server.FindRoles("Filly Scout").ToArray();
            Role[] fillyguide = e.Server.FindRoles("Filly Guide").ToArray();
            return (everypone.Length > 0  && e.User.HasRole(everypone[0])) ||
                   (fillyscout.Length > 0 && e.User.HasRole(fillyscout[0])) ||
                   (fillyguide.Length > 0 && e.User.HasRole(fillyguide[0]));
        }

        bool HasManagerialRolePrerequisites(CommandEventArgs e)
        {
            Role[] managerRole = e.Server.FindRoles("Club Room Manager").ToArray();
            Role[] technicianRole = e.Server.FindRoles("Sweetie-Bot Technician").ToArray();
            return (managerRole.Length > 0 && e.User.HasRole(managerRole[0])) ||
                (technicianRole.Length > 0 && e.User.HasRole(technicianRole[0]));
        }

        private void PonyRolesUpdate(Server server)
        {
            Role[] serverRoles = server.Roles.ToArray();
            List<string> serverRoleNames = new List<string>(serverRoles.Length);
            for (int i = 0; i < server.RoleCount; ++ i)
                serverRoleNames.Add(serverRoles[i].Name);

            for (int i = ponyRoles.Count - 1; i >= 0; --i)
            {
                if (!serverRoleNames.Contains(ponyRoles.Keys.ElementAt(i)) &&
                    !ponyRoles.Keys.ElementAt(i).Equals(PonyRolePrefix + "None"))
                    ponyRoles.Remove(ponyRoles.Keys.ElementAt(i));
            }

            if (!ponyRoles.ContainsKey(PonyRolePrefix + "None"))
                ponyRoles.Add(PonyRolePrefix + "None", "Nopony approves of");

            for (int i = 0; i < server.RoleCount; ++i)
            {
                string serverName = serverRoles[i].Name;
                if (serverName.StartsWith("" + PonyRolePrefix))
                {
                    if (!ponyRoles.ContainsKey(serverName))
                    {
                        string ponyName = serverName.Split(PonyRolePrefix)[1];
                        ponyRoles.Add(serverRoles[i].Name, ponyName + " approves of");
                    }
                }
            }
        }
        
        public void Start()
        {
            pokeTimer.Elapsed += resetpokeState;
            pokeTimer.AutoReset = true;
            pokeTimer.Start();

            if (File.Exists("./ponyroles_messages.txt"))
            {
                ponyRoles = JsonConvert.DeserializeObject<Dictionary<string, string>>
                                    (File.ReadAllText("ponyroles_messages.txt"));
            }

            censorshipManager = new CensorshipManager();
            censorshipManager.Initialize();
            //censorshipManager.ClearDuplicates();
            //censorshipManager.WriteCleanDictionary();

            _client = new DiscordClient();

            _client.UsingAudio(x =>
            {
                x.Mode = AudioMode.Outgoing;
            });

            _client.UsingCommands(x => {
                x.PrefixChar = '!';
                x.HelpMode = HelpMode.Private;
            });
            
            _client.GetService<CommandService>().CreateCommand("EnableFilter")
                .Description("Enables the profanity filter")
                .Do(async e =>
                {
                    if (!e.Message.Channel.IsPrivate)
                    {
                        if (HasManagerialRolePrerequisites(e))
                          {
                            filterEnabled = true;
                            await e.Channel.SendMessage("Profanity filter enabled");
                        }
                        else await e.User.SendMessage("You do not have permission to use that command.");
                    }
                });

            _client.GetService<CommandService>().CreateCommand("DisableFilter")
                .Description("Disables the profanity filter")
                .Do(async e =>
                {
                    if (!e.Message.Channel.IsPrivate)
                    {
                        if (HasManagerialRolePrerequisites(e))
                        {
                            filterEnabled = false;
                            await e.Channel.SendMessage("Profanity filter disabled");
                        }
                        else
                        {
                            await e.User.SendMessage("You do not have permission to use that command.");
                        }
                    }
                });

            
            _client.GetService<CommandService>().CreateCommand("SongRequest")
                .Parameter("SongUrl", ParameterType.Required)
                .Do(async e =>
                {
                    if (!e.Message.Channel.IsPrivate)
                    {
                        string songUrl = e.GetArg("SongUrl");
                        await Audio.Initialize(_client);
                        Audio.Enque(songUrl);

                        await e.Message.Delete();

                        
                        string title = "";
                        string duration = "";
                        if (songUrl.Contains("watch?v="))
                        {
                            System.Diagnostics.Process process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "youtube-dl",
                                Arguments = $"--skip-download --get-title --get-duration {songUrl}",
                                UseShellExecute = false,
                                RedirectStandardOutput = true
                            });

                            string output = process.StandardOutput.ReadToEnd();
                            string[] outputLines = output.Split('\n');
                            title = outputLines[0];
                            duration = outputLines[1];
                        }
                        await e.Channel.SendMessage("Song: " + title + " placed in song queue at #" + Audio.QueueCount() + "\rDuration: " + duration);
                        
                    }
                });


#if DEBUG
            _client.GetService<CommandService>().CreateCommand("test")
                .Do(async e =>
                {
                    await e.Channel.SendMessage("\\expressionless");
                });
#endif
            _client.GetService<CommandService>().CreateCommand("Rules")
                .Description("Display the server rules.")
                .Do(async e =>
                {
                    await e.Channel.SendMessage("General rules:\n- NOTHING illegal (as in no IRL little nekkid girls) \n- No child model shots, like provocative poses, swimsuits, or underwear. Nothing against it, but it's not what this server is about and makes some uncomfortable. \n- Listen to the Club Room Managers\n- Lastly, don't be an ass. <:rainbowdetermined2:250101115872346113>");
                });

            _client.GetService<CommandService>().CreateCommand("timeout")
                .Parameter("TimeoutUser", ParameterType.Required)
                .Parameter("TimeoutLength", ParameterType.Required)
                .Do(async e =>
                {
                    if (HasManagerialRolePrerequisites(e))
                    {
                        Timer timeoutCounter = new Timer(double.Parse(e.GetArg("TimeoutLength")) * 1000);

                        ulong userID = ulong.Parse(e.GetArg("TimeoutUser").Trim('<', '>', '@'));

                        User user = e.Server.GetUser(userID);
                        Console.Write(user.ToString());

                        Role banRole = e.Server.GetRole(251120565358821376);

                        await user.AddRoles(banRole);

                        timeoutCounter.Elapsed += (sender, ev) => endTimeout(sender, ev, e, userID, banRole);
                        timeoutCounter.Enabled = true;
                        timeoutCounter.AutoReset = false;
                        timeoutCounter.Start();
                    }
                    else
                    {
                        await e.Channel.SendMessage("You are not a mod.");
                    }
                });

            _client.GetService<CommandService>().CreateCommand("PonyRoles")
                .Do(async e =>
                {
                    PonyRolesUpdate(e.Server);

                    string availablePonies = "Available ponies: ";
                    for (int i = 0; i < ponyRoles.Count; ++i)
                    {
                        availablePonies += ponyRoles.Keys.ElementAt(i).Split(PonyRolePrefix)[1];
                        if (i != ponyRoles.Count - 1)
                            availablePonies += ", ";
                    }

                    await e.User.SendMessage(availablePonies);
                });
            
            _client.GetService<CommandService>().CreateCommand("PonyRole")
                .Parameter("ChosenPony", ParameterType.Required)
                .Parameter("Message", ParameterType.Unparsed)
                .Description("Select a pony role. Use !ponyroles command to see the list of ponies available.")
                .Do(async e =>
                {
                    if (!e.Message.Channel.IsPrivate)
                    {
                        PonyRolesUpdate(e.Server);

                        string chosenPony = e.GetArg("ChosenPony").ToLower();
                        chosenPony = ("" + PonyRolePrefix) + char.ToUpper(chosenPony[0]) + chosenPony.Substring(1, chosenPony.Length - 1);
                        if (ponyRoles.ContainsKey(chosenPony))
                        {
                            Role[] assignedRole = e.Server.FindRoles(chosenPony).ToArray();
                            if (chosenPony.Equals(PonyRolePrefix + "None") || assignedRole.Length > 0)
                            {
                                if (HasPonyRolePrerequisites(e))
                                {
                                    string message = e.GetArg("Message");
                                    if (message.Length > 0 && message.StartsWith("msg ", StringComparison.OrdinalIgnoreCase) && HasManagerialRolePrerequisites(e))
                                    {
                                        string ponymessage = message.Split(new string[] { "msg "}, StringSplitOptions.None)[1];
                                        ponyRoles[chosenPony] = ponymessage;
                                        await e.User.SendMessage(chosenPony.Split(PonyRolePrefix)[1] + " now says " + ponymessage + " the user.");
                                        string json = JsonConvert.SerializeObject(ponyRoles);
                                        File.WriteAllText("ponyroles_messages.txt", json);
                                    }
                                    else if (chosenPony.Equals(PonyRolePrefix + "None") || !e.User.HasRole(assignedRole[0]))
                                    {
                                        for (int i = 0; i < ponyRoles.Count; ++i)
                                        {
                                            Role[] ponyrole = e.Server.FindRoles(ponyRoles.Keys.ElementAt(i)).ToArray();
                                            if (ponyrole.Length > 0 && e.User.HasRole(ponyrole[0]))
                                            {
                                                await e.User.RemoveRoles(ponyrole);
                                                System.Threading.Thread.Sleep(250);
                                            }
                                        }

                                        string ponyString = ponyRoles[chosenPony];
                                        if (!chosenPony.Equals(PonyRolePrefix + "None"))
                                            await e.User.AddRoles(assignedRole);
                                        System.Threading.Thread.Sleep(250);
                                        await e.Channel.SendMessage(string.Format("{0} {1}.", ponyString, e.User.ToString()));
                                    }
                                    else await e.Channel.SendMessage("You already have that role, silly.");
                                }
                                else await e.Channel.SendMessage("You need a role first.");
                            }
                            else await e.Channel.SendMessage("That pony has not been added to the discord group yet.");
                        }
                        else await e.Channel.SendMessage("That pony is not available at the moment.");
                    }
                });

            _client.GetService<CommandService>().CreateCommand("PonyRoll")
                .Description("Selects a random pony role. Use !ponyroles command to see the list of ponies available.")
                .Do(async e =>
                {
                    if (!e.Message.Channel.IsPrivate)
                    {
                        PonyRolesUpdate(e.Server);
                        Random rand = new Random();
                        string chosenPony = ponyRoles.Keys.ElementAt(rand.Next(1, ponyRoles.Count));

                        Role[] assignedRole = e.Server.FindRoles(chosenPony).ToArray();
                        if (HasPonyRolePrerequisites(e))
                        {
                            for (int i = 0; i < ponyRoles.Count; ++i)
                            {
                                Role[] ponyrole = e.Server.FindRoles(ponyRoles.Keys.ElementAt(i)).ToArray();
                                if (ponyrole.Length > 0 && e.User.HasRole(ponyrole[0]))
                                {
                                    await e.User.RemoveRoles(ponyrole);
                                    System.Threading.Thread.Sleep(250);
                                }
                            }

                            string ponyString = ponyRoles[chosenPony];
                            await e.User.AddRoles(assignedRole);
                            System.Threading.Thread.Sleep(250);
                            await e.Channel.SendMessage(string.Format("{0} {1}.", ponyString, e.User.ToString()));
                        }
                        else await e.Channel.SendMessage("You need a role first.");
                    }
                });
            

            _client.GetService<CommandService>().CreateCommand("poke")
                .Description("Does... Somthing. <:raritywink:250101117055139840>")
                .Do(async e =>
                {
                    if (nsfwChannels.Contains(e.Channel.Name))
                    {
                        switch (pokeState)
                        {
                            case 0:
                                await e.Channel.SendMessage("Eep!");
                                break;
                            case 1:
                                await e.Channel.SendMessage("H-hey!");
                                break;
                            case 2:
                                await e.Channel.SendMessage("Oh~");
                                break;
                            case 3:
                                await e.Channel.SendMessage("At least take me on a date first...");
                                break;
                            case 4:
                                await e.Channel.SendMessage("Mmmph, keep it up.");
                                break;
                            case 5:
                                await e.Channel.SendMessage("D-don't tell Rarity about this. Ohh!");
                                break;
                            case 6:
                                await e.Channel.SendMessage("... Ohh, im gonna be sore for a bit...");
                                break;
                            default:
                                await e.Channel.SendMessage("Could you take a break? I'm still sore...");
                                break;
                        }
                        pokeState++;
                    }
                    else
                    {
                        await e.Channel.SendMessage("Eep!");
                    }
                });

            _client.GetService<CommandService>().CreateCommand("Iam18")
                .Description("Confirm you are 18, and add puts you in the 18+ channels.")
                .Do(async e =>
                {
                    if (!e.Message.Channel.IsPrivate)
                    {
                        if (!e.User.HasRole(e.Server.FindRoles("Filly Scout").ToArray()[0]))
                        {
                            await e.User.AddRoles(e.Server.FindRoles("@everypone").ToArray());
                            await e.Channel.SendMessage(string.Format("There you go {0}.", e.User.ToString()));
                            System.Threading.Thread.Sleep(100);
                            await e.Channel.SendMessage("Now you can join the loli/shota channels with `!Loli join`");
                        }
                        else
                        {
                            await e.Channel.SendMessage("Don't try lying to me, I know you're not 18.");
                        }
                    }
                });

            _client.GetService<CommandService>().CreateGroup("Loli", loli =>
            {
                loli.CreateCommand("join")
                    .Description("If you have confirmed your age, join the loli/shota chats.")
                    .Do(async e =>
                    {
                        if (!e.Message.Channel.IsPrivate)
                        {
                            if (e.User.HasRole(e.Server.FindRoles("@everypone").ToArray()[0]))
                            {
                                await e.User.AddRoles(e.Server.FindRoles("obsessed with hands").ToArray());
                                await e.Channel.SendMessage("You're in the loli channels now. :raritywink: \nLeave any time with `!loli leave`");
                            }
                            else
                            {
                                await e.Channel.SendMessage("You have to confirm you are 18 first silly~");
                            }
                        }
                    });

                loli.CreateCommand("leave")
                    .Description("Remove your access to the loli/shota chats.")
                    .Do(async e =>
                    {
                        if (!e.Message.Channel.IsPrivate)
                        {
                            await e.User.RemoveRoles(e.Server.FindRoles("obsessed with hands").ToArray());
                            await e.Channel.SendMessage("Im sad to see you go... Come back any time!");
                        }
                    });
            });

            _client.MessageUpdated += async (s, e) =>
            {
                if (!e.After.IsAuthor)
                {
                    if (filterEnabled && !e.Channel.IsPrivate)
                    {
                        if (!nsfwChannels.Contains(e.Channel.Name) && e.Channel.Name != "staff-eyes-only")
                        {
                            string filtered = censorshipManager.censor.CensorMessage(e.After.Text);
                            if (filtered != null && filtered != e.After.Text)
                            {
                                int count = GetProfanityCount(e.User.Name);
                                string outputMessage = GetOutputMessage(e.User.Name, count, e.After.Text, filtered);

                                if (userProfanityCount[e.User.Name].DeleteProfanity)
                                    await e.After.Delete();

                                if (userProfanityCount[e.User.Name].Report)
                                    await e.Channel.SendMessage(e.User.Name + " made Fluttershy cry from excessive use of profanity! <:fluttercry:250101114140098562> \rFuture messages from this user containing profanity will be automatically deleted.");

                                await e.User.SendMessage(profaneMessageResponses[count]);
                                await e.User.SendMessage(outputMessage.Truncate(2000));
                            }
                        }
                    }
                }
            };

            _client.MessageReceived += async (s, e) =>
            {
                if (!e.Message.IsAuthor)
                {
                    if (filterEnabled && !e.Channel.IsPrivate)
                    {
                        if (!nsfwChannels.Contains(e.Channel.Name) && e.Channel.Name != "staff-eyes-only")
                        {
                            string filtered = censorshipManager.censor.CensorMessage(e.Message.Text);
                            if (filtered != null && filtered != e.Message.Text)
                            {
                                int count = GetProfanityCount(e.User.Name);
                                Console.WriteLine(e.Message.ToString());
                                string outputMessage = GetOutputMessage(e.User.Name, count, e.Message.Text, filtered);
                                
                                if (userProfanityCount[e.User.Name].DeleteProfanity)
                                    await e.Message.Delete();

                                if (userProfanityCount[e.User.Name].Report)
                                    await e.Channel.SendMessage(e.User.Name + " made Fluttershy cry from excessive use of profanity! <:fluttercry:250101114140098562>");

                                await e.User.SendMessage(profaneMessageResponses[count]);
                                await e.User.SendMessage(outputMessage.Truncate(2000));
                            }
                        }
                    }
                    


                    if (e.Message.RawText.StartsWith("hey sweetie, ", StringComparison.OrdinalIgnoreCase) |
                        e.Message.RawText.StartsWith("sweetie, ", StringComparison.OrdinalIgnoreCase))
                    {
                        if (e.Message.RawText.EndsWith("?", StringComparison.OrdinalIgnoreCase))
                        {
                            Random rand = new Random();
                            if (nsfwChannels.Contains(e.Channel.Name))
                            {
                                int magic8 = rand.Next(0, magic8nsfw.Union(magic8phrases).ToArray().Count());
                                await e.Channel.SendMessage(magic8nsfw.Union(magic8phrases).ToArray()[magic8]);
                            }
                            else
                            {
                                int magic8 = rand.Next(1, magic8phrases.Count());
                                await e.Channel.SendMessage(magic8phrases[magic8]);
                            }
                        }
                    }

                    if (e.Channel.IsPrivate)
                    {
                        Console.WriteLine("{0}: {1}", e.User.Name, e.Message.Text);
                    }

                    if (e.Message.Text.StartsWith("_") & e.Message.Text.EndsWith("_") & e.Message.Text.ToLower().Contains("sweetie"))
                    {
                        Random rand = new Random();
                        if (nsfwChannels.Contains(e.Channel.Name))
                        {
                            int responseNum = rand.Next(0, meResponsesLewd.Union(meResponsesClean).ToArray().Count());
                            await e.Channel.SendMessage(string.Format("_" + meResponsesLewd.Union(meResponsesClean).ToArray()[responseNum] + "_", e.User.Name));
                        }
                        else
                        {
                            int responseNum = rand.Next(1, meResponsesClean.Count());
                            await e.Channel.SendMessage(string.Format("_" + meResponsesClean[responseNum] + "_", e.User.Name));
                        }

                    }
                }
            };

            _client.UserJoined += async (s, e) =>
            {
                Console.WriteLine("New person! Said hi to {0}.", e.User.Name);
                await e.User.SendMessage("Welcome to the Crusaders Clubhouse!");
                System.Threading.Thread.Sleep(500);
                await e.User.SendMessage("Im the local bot, Sweetie-Bot! \n I do a *lot* of cool things.");
                System.Threading.Thread.Sleep(500);
                await e.User.SendMessage("If you are 18, confirm it by typing '!Iam18' in the general chat and I will add you to the 18+ channels.");
                System.Threading.Thread.Sleep(500);
                await e.User.SendMessage("(Don't lie, if it is revealed that you confirmed your age falsely, you could get banned.)");
                System.Threading.Thread.Sleep(500);
                await e.User.SendMessage("I can't tell you everything I can do right now, that would take up too much space! Type '!rules' to get the server rules at anytime, and '!help' to find out all the other stuff I can do.");
                System.Threading.Thread.Sleep(500);
                await e.User.SendMessage("I hope you have a fun time here!");
            };

            _client.ExecuteAndWait(async () => {

                string BOT_TOKEN;

                if (File.Exists(BOT_TOKEN_FILE))
                {
                    try
                    {
                        BOT_TOKEN = File.ReadLines(BOT_TOKEN_FILE).Take(1).First();
                    }
                    catch (FileNotFoundException)
                    {
                        throw;
                    }

                    await _client.Connect(BOT_TOKEN, TokenType.Bot);
                }
                else
                {
                    Console.WriteLine("Error: Token file does not exist. Press Enter to close program.");
                    Console.ReadLine();
                }
            });
        }
    }
}
