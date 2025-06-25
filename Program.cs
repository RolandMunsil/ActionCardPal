using ImGuiNET;
using RetroRoulette;
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid;
using System.Net.NetworkInformation;

namespace ActionCardPal
{
    internal class Program
    {
        static ImFontPtr font20;
        static ImFontPtr font30;
        static ImFontPtr font40;

        static bool renderImguiDemo = false;

        static void Main(string[] args)
        {
            Sdl2Window window;
            GraphicsDevice gd;

            // Create window, GraphicsDevice, and all resources necessary

            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "ActionCardPal"),
                new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true),
                out window,
                out gd);

            ImGui.CreateContext();
            ImGuiIOPtr io = ImGui.GetIO();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;
            io.Fonts.Flags |= ImFontAtlasFlags.NoBakedLines;

            font20 = io.Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\segoeui.ttf", 20);
            font30 = io.Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\segoeui.ttf", 30);
            font40 = io.Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\segoeui.ttf", 40);

            CommandList cl = gd.ResourceFactory.CreateCommandList();
            ImGuiController controller = new ImGuiController(gd, gd.MainSwapchain.Framebuffer.OutputDescription, window.Width, window.Height);

            window.Resized += () =>
            {
                gd.MainSwapchain.Resize((uint)window.Width, (uint)window.Height);
                controller.WindowResized(window.Width, window.Height);
            };

            Stopwatch dtStopwatch = Stopwatch.StartNew();

            // Main application loop
            while (window.Exists)
            {
                float deltaTime = (float)dtStopwatch.Elapsed.TotalSeconds;
                dtStopwatch.Restart();
                InputSnapshot snapshot = window.PumpEvents();
                if (!window.Exists) { break; }
                controller.Update(deltaTime, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

                RenderUI();

                cl.Begin();
                cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
                cl.ClearColorTarget(0, new RgbaFloat(1, 1, 1, 1));
                controller.Render(gd, cl);
                cl.End();
                gd.SubmitCommands(cl);
                gd.SwapBuffers(gd.MainSwapchain);
            }

            // Clean up Veldrid resources
            gd.WaitForIdle();
            controller.Dispose();
            cl.Dispose();
            gd.Dispose();
        }

        enum Suit
        { 
            Joker = -1,
            Clubs = 0,
            Diamonds,
            Hearts,
            Spades,
        }

        enum Rank
        {
            _2 = 2,
            _3,
            _4,
            _5,
            _6,
            _7,
            _8,
            _9,
            _10,
            Jack,
            Queen,
            King,
            Ace,
        }

        const Rank RANK_JOKER_1 = Rank._2;
        const Rank RANK_JOKER_2 = Rank._3;

        readonly record struct Card(Suit Suit, Rank Rank)
        {
            internal int OrderVal
            { 
                get 
                { 
                    Debug.Assert(Suit != Suit.Joker); 
                    return (int)Suit * 13 + (int)Rank; 
                } 
            }

            public static bool operator >(Card left, Card right) => left.OrderVal > right.OrderVal;
            public static bool operator <(Card left, Card right) => left.OrderVal < right.OrderVal;
        }

        static List<Card> CreateDeck()
        {
            return new List<Card>()
            {
                new Card(Suit.Spades, Rank.Ace),
                new Card(Suit.Spades, Rank.King),
                new Card(Suit.Spades, Rank.Queen),
                new Card(Suit.Spades, Rank.Jack),
                new Card(Suit.Spades, Rank._10),
                new Card(Suit.Spades, Rank._9),
                new Card(Suit.Spades, Rank._8),
                new Card(Suit.Spades, Rank._7),
                new Card(Suit.Spades, Rank._6),
                new Card(Suit.Spades, Rank._5),
                new Card(Suit.Spades, Rank._4),
                new Card(Suit.Spades, Rank._3),
                new Card(Suit.Spades, Rank._2),
                new Card(Suit.Hearts, Rank.Ace),
                new Card(Suit.Hearts, Rank.King),
                new Card(Suit.Hearts, Rank.Queen),
                new Card(Suit.Hearts, Rank.Jack),
                new Card(Suit.Hearts, Rank._10),
                new Card(Suit.Hearts, Rank._9),
                new Card(Suit.Hearts, Rank._8),
                new Card(Suit.Hearts, Rank._7),
                new Card(Suit.Hearts, Rank._6),
                new Card(Suit.Hearts, Rank._5),
                new Card(Suit.Hearts, Rank._4),
                new Card(Suit.Hearts, Rank._3),
                new Card(Suit.Hearts, Rank._2),
                new Card(Suit.Diamonds, Rank.Ace),
                new Card(Suit.Diamonds, Rank.King),
                new Card(Suit.Diamonds, Rank.Queen),
                new Card(Suit.Diamonds, Rank.Jack),
                new Card(Suit.Diamonds, Rank._10),
                new Card(Suit.Diamonds, Rank._9),
                new Card(Suit.Diamonds, Rank._8),
                new Card(Suit.Diamonds, Rank._7),
                new Card(Suit.Diamonds, Rank._6),
                new Card(Suit.Diamonds, Rank._5),
                new Card(Suit.Diamonds, Rank._4),
                new Card(Suit.Diamonds, Rank._3),
                new Card(Suit.Diamonds, Rank._2),
                new Card(Suit.Clubs, Rank.Ace),
                new Card(Suit.Clubs, Rank.King),
                new Card(Suit.Clubs, Rank.Queen),
                new Card(Suit.Clubs, Rank.Jack),
                new Card(Suit.Clubs, Rank._10),
                new Card(Suit.Clubs, Rank._9),
                new Card(Suit.Clubs, Rank._8),
                new Card(Suit.Clubs, Rank._7),
                new Card(Suit.Clubs, Rank._6),
                new Card(Suit.Clubs, Rank._5),
                new Card(Suit.Clubs, Rank._4),
                new Card(Suit.Clubs, Rank._3),
                new Card(Suit.Clubs, Rank._2),
                new Card(Suit.Joker, RANK_JOKER_1),
                new Card(Suit.Joker, RANK_JOKER_2),
            };
        }

        class Actor
        {
            // TODO color? icon?

            public string name;
            public List<Card> cards = new List<Card>();
            Card? cardSelected = null;

            public Actor(string name)
            {
                this.name = name;
            }

            public void GiveCard(Card card)
            {
                cards.Add(card);

                if (cardSelected == null)
                {
                    cardSelected = card;
                }
            }

            public void DiscardCard(Card card)
            {
                Debug.Assert(cards.Contains(card));
                cards.Remove(card);

                if (cardSelected == card)
                {
                    cardSelected = null;
                }

                if (cardSelected == null && cards.Count > 0)
                {
                    cardSelected = cards[0];
                }
            }
        }

        static List<Card> deck = CreateDeck();
        static List<Actor> actors = new List<Actor>();
        static List<Card> discardPile = new List<Card>();


        static Card TakeTopCard()
        {
            Card card = deck[^1];
            deck.RemoveAt(deck.Count - 1);
            return card;
        }

        // participant:
        //  list of cards
        //  draw card
        //  discard card
        //  set card as initiative card

        static void RenderUI()
        {
            ImGuiViewportPtr viewportptr = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewportptr.WorkPos);
            ImGui.SetNextWindowSize(viewportptr.WorkSize);
            ImGui.SetNextWindowViewport(viewportptr.ID);

            if (ImGui.Begin("Main", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove))
            {
                foreach (Actor actor in actors)
                {
                    ImGui.PushID($"{actors.IndexOf(actor)}");

                    ImGui.SetNextItemWidth(ImGui.CalcTextSize(actor.name).X + ImGui.GetStyle().FramePadding.X * 2);
                    ImGui.InputText($"##name", ref actor.name, (uint)actor.name.Length + 1024, ImGuiInputTextFlags.NoHorizontalScroll);

                    ImGui.SameLine();

                    if (ImGui.Button("Deal card"))
                    {
                        actor.GiveCard(TakeTopCard());
                    }

                    ImGui.Indent();
                    
                    foreach (Card card in actor.cards.ToArray())
                    {
                        ImGui.PushID($"{actor.cards.IndexOf(card)}");

                        ImGui.TextUnformatted($"{card}");
                        ImGui.SameLine();

                        if (ImGui.Button("Discard"))
                        {
                            actor.DiscardCard(card);
                            discardPile.Add(card);
                        }

                        ImGui.PopID();
                    }

                    ImGui.Unindent();

                    ImGui.PopID();
                }

                if (ImGui.Button("Add actor"))
                {
                    actors.Add(new Actor("New actor"));
                }

                ImGui.Separator();

                if (ImGui.Button("Reshuffle deck"))
                {
                    discardPile.Clear();
                    deck = CreateDeck();
                    Random.Shared.Shuffle<Card>(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(deck));
                }

                ImGui.Separator();

                foreach (Card card in deck)
                {
                    ImGui.TextUnformatted($"{card}");
                }
            }
            ImGui.End();
        }
    }
}
