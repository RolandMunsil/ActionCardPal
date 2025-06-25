using ImGuiNET;
using RetroRoulette;
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid;
using System.Net.NetworkInformation;
using System.Numerics;
using Newtonsoft.Json.Linq;
using static System.Reflection.Metadata.BlobBuilder;

namespace ActionCardPal
{
    internal class Program
    {
        static ImFontPtr SEGOE_UI_20;
        static ImFontPtr SEGOE_UI_SYMBOL_50;
        static ImFontPtr SEGOE_UI_SYMBOL_70;

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

            SEGOE_UI_20 = io.Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\segoeui.ttf", 20);

            unsafe
            {
                fixed (ushort* pRange = new ushort[] { 0x20, 0x7f, 0x2660, 0x2667, 0x0 })
                {
                    SEGOE_UI_SYMBOL_50 = io.Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\seguisym.ttf", 50, new ImFontConfigPtr(), (nint)pRange);
                    SEGOE_UI_SYMBOL_70 = io.Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\seguisym.ttf", 70, new ImFontConfigPtr(), (nint)pRange);
                }
            }

            ImGuiStylePtr imstyle = ImGui.GetStyle();
            imstyle.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.9f, 0.9f, 0.9f, 1.0f);
            imstyle.Colors[(int)ImGuiCol.Text] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            imstyle.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);

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
            Clubs = 0,
            Diamonds,
            Hearts,
            Spades,
            Joker,
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

        class CardStack
        {
            public IEnumerable<Card> Cards => ALL_CARDS.Where(card => card.Owner == this);
            public IEnumerable<Card> CardsTopToBottom => Cards.OrderByDescending(card => card.Height);
            public IEnumerable<Card> CardsBestToWorst => Cards.OrderByDescending(card => card.InitiativeValue);

            public bool HasAnyCards => Cards.Any();
            public Card? TopCard => Cards.MaxBy(card => card.Height);

            public bool HasCard(Card card) => Cards.Contains(card);
        }

        static CardStack CSTACK_DECK = new CardStack();
        static CardStack CSTACK_DISCARD = new CardStack();

        record class Card(Suit Suit, Rank Rank)
        {
            public CardStack Owner { get; set; } = CSTACK_DECK;
            public int Height { get; set; }

            public int InitiativeValue => (int)Suit * 13 + (int)Rank;

            public override string ToString()
            {
                if (Suit == Suit.Joker)
                {
                    return Rank == RANK_JOKER_1 ? "Joker 1" : "Joker 2";
                }
                else
                {
                    string rankStr = Rank <= Rank._10 ? ((int)Rank).ToString() : Rank.ToString();
                    return $"{rankStr} of {Suit}";
                }
            }
        }

        static Card[] ALL_CARDS =
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
        
        class Actor
        {
            // TODO color? icon?

            public string name;
            public CardStack cstack = new CardStack();

            private Card? lastSelectedCard;

            public Card? SelectedCard
            {
                get
                {
                    if (lastSelectedCard != null && !cstack.HasCard(lastSelectedCard))
                    {
                        lastSelectedCard = null;
                    }

                    if (lastSelectedCard == null && cstack.HasAnyCards)
                    {
                        lastSelectedCard = cstack.TopCard;
                    }

                    return lastSelectedCard;
                }
                set
                {
                    lastSelectedCard = value;
                }
            }

            public Actor(string name)
            {
                this.name = name;
            }
        }

        static List<Actor> actors = new List<Actor>();

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

                    ImGui.BeginGroup();

                    ImGui.SetNextItemWidth(ImGui.CalcTextSize(actor.name).X + ImGui.GetStyle().FramePadding.X * 2);
                    ImGui.InputText($"##name", ref actor.name, (uint)actor.name.Length + 1024, ImGuiInputTextFlags.NoHorizontalScroll);

                    // ImGui.SameLine();

                    ImGui.BeginDisabled(!CSTACK_DECK.HasAnyCards);
                    if (ImGui.Button("Deal card"))
                    {
                        CSTACK_DECK.TopCard!.Owner = actor.cstack;
                    }
                    ImGui.EndDisabled();

                    ImGui.Indent();
                    
                    foreach (Card card in actor.cstack.CardsBestToWorst)
                    {
                        ImGui.PushID($"{card}");

                        ImGui.BeginGroup();

                        RenderCard(card);

                        if (ImGui.Button("Discard", new Vector2(ImGui.GetItemRectSize().X, 0)))
                        {
                            card.Owner = CSTACK_DISCARD;
                            card.Height = CSTACK_DISCARD.TopCard!.Height + 1;
                        }

                        ImGui.EndGroup();

                        ImGui.SameLine();

                        ImGui.PopID();
                    }

                    ImGui.NewLine();

                    ImGui.Unindent();

                    ImGui.EndGroup();

                    ImGui.PopID();

                    ImGui.SameLine();
                }

                ImGui.NewLine();

                if (ImGui.Button("Add actor"))
                {
                    actors.Add(new Actor("New actor"));
                }

                ImGui.Separator();

                if (ImGui.Button("Collect & reshuffle deck"))
                {
                    int[] heights = Enumerable.Range(1, ALL_CARDS.Length).ToArray();
                    Random.Shared.Shuffle(heights);

                    for (int iCard = 0; iCard < ALL_CARDS.Length; iCard++)
                    {
                        ALL_CARDS[iCard].Owner = CSTACK_DECK;
                        ALL_CARDS[iCard].Height = heights[iCard];
                    }
                }

                ImGui.Separator();

                if (ImGui.BeginTable("split", 2))
                {
                    ImGui.TableNextColumn();

                    foreach (Card card in CSTACK_DECK.CardsTopToBottom)
                    {
                        ImGui.TextUnformatted($"{card}");
                    }

                    ImGui.TableNextColumn();

                    foreach (Card card in CSTACK_DISCARD.CardsTopToBottom)
                    {
                        ImGui.TextUnformatted($"{card}");
                    }

                    ImGui.EndTable();
                }

            }
            ImGui.End();
        }

        static Dictionary<Suit, string> suitToIcon = new Dictionary<Suit, string>()
        {
            { Suit.Clubs, "♣" },
            { Suit.Diamonds, "♦" },
            { Suit.Hearts, "♥" },
            { Suit.Spades, "♠" },
            { Suit.Joker, "J" },
        };

        static Dictionary<Suit, Vector4> suitToColor = new Dictionary<Suit, Vector4>()
        {
            { Suit.Clubs, new Vector4(0.4f, 0.4f, 0.4f, 1.0f) },
            { Suit.Diamonds, new Vector4(1.0f, 0.4f, 0.4f, 1.0f) },
            { Suit.Hearts, new Vector4(1.0f, 0.0f, 0.0f, 1.0f) },
            { Suit.Spades, new Vector4(0.0f, 0.0f, 0.0f, 1.0f) },
            { Suit.Joker, new Vector4(1.0f, 0.0f, 1.0f, 1.0f) },
        };

        static Dictionary<Rank, string> rankToIcon = new Dictionary<Rank, string>()
        {
            { Rank._2, "2" },
            { Rank._3, "3" },
            { Rank._4, "4" },
            { Rank._5, "5" },
            { Rank._6, "6" },
            { Rank._7, "7" },
            { Rank._8, "8" },
            { Rank._9, "9" },
            { Rank._10, "10" },
            { Rank.Jack, "J" },
            { Rank.Queen, "Q" },
            { Rank.King, "K" },
            { Rank.Ace, "A" },
        };

        static void RenderCard(Card card)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, suitToColor[card.Suit]);
            ImGui.BeginGroup();

            Vector2 cardSize = new Vector2(80, 80 * 1.4f);

            ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + cardSize, 0xffffffff);

            Vector2 cursorPosTopLeft = ImGui.GetCursorPos();

            ImGui.PushFont(SEGOE_UI_SYMBOL_70);
            {
                string strSuit = suitToIcon[card.Suit];
                Vector2 strSuitSize = ImGui.CalcTextSize(strSuit);

                ImGui.SetCursorPos(cursorPosTopLeft + new Vector2(cardSize.X / 2 - strSuitSize.X / 2, cardSize.Y * 0.25f - strSuitSize.Y / 2));
                ImGui.TextUnformatted(strSuit);
            }
            ImGui.PopFont();

            ImGui.PushFont(SEGOE_UI_SYMBOL_50);
            {
                string strRank = rankToIcon[card.Rank];
                Vector2 strRankSize = ImGui.CalcTextSize(strRank);

                ImGui.SetCursorPos(cursorPosTopLeft + new Vector2(cardSize.X / 2 - strRankSize.X / 2, cardSize.Y * 0.75f - strRankSize.Y / 2));
                ImGui.TextUnformatted(strRank);
            }
            ImGui.PopFont();

            ImGui.SetCursorPos(cursorPosTopLeft);
            ImGui.Dummy(cardSize);

            ImGui.EndGroup();
            ImGui.PopStyleColor();
        }
    }
}
