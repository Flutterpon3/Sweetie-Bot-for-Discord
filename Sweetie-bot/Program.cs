using System;
using System.Collections.Generic;
using System.Linq;


namespace Sweetie_bot
{

    using Discord;
    using Discord.Commands;
    using System.IO;
    using System.Timers;

    public class ProfanityCounter
    {
        private Timer profanityTimer;
        int count;

        public ProfanityCounter()
        {
            count = 0;
            profanityTimer = new Timer(20 * 1000);
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
            profanityTimer.Stop();
            profanityTimer.Start();
            count++;
            if (count > 6) count = 6;
            return count;
        }

        private void sub(Object source, ElapsedEventArgs e)
        {
            count--;
            if (count < 0) count = 0;
        }
    }

    class Program
    {

        const string BOT_TOKEN_FILE = "./discord_token.txt";

        static void Main(string[] args) => new Program().Start();

        static int pokeState = 0;

        private void resetpokeState(Object source, ElapsedEventArgs e)
        {
            pokeState = 0;
        }

        private static Timer pokeTimer = new Timer(60 * 1000);

        private DiscordClient _client;

        private CensorshipManager censorshipManager;

        List<string> meResponsesClean = new List<string>()
        {
            "gasps, \"Hey!\"",
            "shrinks away from {0}.",
            "tilts head, staring at {0} curiously.",
            "goes to {0}, nuzzling their leg.",
            "smiles happily.",
            "giggles to herself.",
            "falls asleep on {0}"
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
            "non-mlp-nsfw",
            "filly-only",
            "filly-x-colt",
            "colt-x-only",
            "filly-x-male",
            "filly-x-female",
            "colt-x-male",
            "colt-x-female",
            "mlp-loli",
            "mlp-shota",
            "mlp-loli-x-shota"
        });

        Dictionary<string, ProfanityCounter> userProfanityCount = new Dictionary<string, ProfanityCounter>();

        List<string> profaneMessageResponses = new List<string>(new string[]
        {
            "Your message contained profanity, please replace the underlined content or post your message in a NSFW channel.",
            "I asked you nicely before, would you please replace the profane content of your message?",
            "Pretty please with a cherry on top?",
            "I'll ask again... PLEASE replace the profane content in your message!",
            "You're making fun of me, aren't you?",
            ".....",
            "You know what... I showed that last message of yours to a certain somepony, and do you want to know what you've done?\rYou made Fluttershy cry!"
        });

        public void Start()
        {
            pokeTimer.Elapsed += resetpokeState;
            pokeTimer.AutoReset = true;
            pokeTimer.Start();


            censorshipManager = new CensorshipManager();
            censorshipManager.Initialize();

            _client = new DiscordClient();

            _client.UsingCommands(x => {
                x.PrefixChar = '!';
                x.HelpMode = HelpMode.Private;
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

            _client.MessageReceived += async (s, e) =>
            {
                if (!e.Message.IsAuthor)
                {

                    if (!nsfwChannels.Contains(e.Channel.Name))
                    {
                        string filtered = censorshipManager.censor.CensorMessage(e.Message.Text);
                        if (!filtered.Equals(e.Message.Text))
                        {
                            await e.Message.Delete();

                            int count = 0;
                            if (!userProfanityCount.ContainsKey(e.User.Name))
                                userProfanityCount.Add(e.User.Name, new ProfanityCounter());
                            else
                                count = userProfanityCount[e.User.Name].add();

                            //string underlinedMessage = censorshipManager.censor.Underline(e.Message.Text, filtered);
                            string outputMessage;
                            if (count < 3) outputMessage = censorshipManager.censor.Underline(e.Message.Text, filtered);
                            else if (count < 6) outputMessage = censorshipManager.censor.BoldUnderline(e.Message.Text, filtered);
                            else
                            {
                                outputMessage = censorshipManager.censor.FlutterCryFilter(e.Message.Text, filtered);
                                await e.Channel.SendMessage(e.User.Name + " made Fluttershy cry from excessive use of profanity! :fluttercry:");
                            }

                            await e.User.SendMessage(profaneMessageResponses[count] + "\r" + outputMessage);
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
