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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.Runtime.Versioning;

namespace ActionCardPal
{
    internal class Program
    {
        //////

        class CardStack
        {
            public IEnumerable<Card> Cards => ALL_CARDS.Where(card => card.Owner == this);
            public IEnumerable<Card> CardsTopToBottom => Cards.OrderByDescending(card => card.Height);
            public IEnumerable<Card> CardsBottomToTop => Cards.OrderBy(card => card.Height);
            public IEnumerable<Card> CardsBestToWorst => Cards.OrderByDescending(card => card.InitiativeValue);

            public bool HasAnyCards => Cards.Any();
            public Card? TopCard => Cards.MaxBy(card => card.Height);

            public bool HasCard(Card card) => Cards.Contains(card);

            public void Render(Vector2 bottomLeft)
            {
                int dy = 0;

                foreach (Card card in CardsBottomToTop)
                {
                    ImGui.SetCursorPosX(bottomLeft.X);
                    ImGui.SetCursorPosY(bottomLeft.Y - Card.RENDER_HEIGHT - dy);
                    card.Render();

                    if (card != TopCard)
                    {
                        ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), Rgba32(0, 0, 0, 0.13));
                    }

                    dy += 1;
                }
            }
        }

        static readonly CardStack CSTACK_DECK = new CardStack();
        static readonly CardStack CSTACK_DISCARD = new CardStack();

        //////

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
            Joker,
        }

        record class Card(Suit Suit, Rank Rank)
        {
            public CardStack Owner { get; set; } = CSTACK_DECK;
            public int Height { get; set; } = 0;
            public bool FaceUp { get; set; } = false;

            public int InitiativeValue => (int)Rank * 5 + (int)Suit;

            public void Discard()
            {
                Owner = CSTACK_DISCARD;
                Height = CSTACK_DISCARD.TopCard!.Height + 1;
                FaceUp = true;
            }

            public override string ToString()
            {
                if (Rank == Rank.Joker)
                {
                    return "Joker";
                }
                else
                {
                    string rankStr = Rank <= Rank._10 ? ((int)Rank).ToString() : Rank.ToString();
                    return $"{rankStr} of {Suit}";
                }
            }

            //

            public const float RENDER_WIDTH = 60;
            public const float RENDER_HEIGHT = RENDER_WIDTH * 1.4f;
            public static readonly Vector2 RENDER_SIZE = new(RENDER_WIDTH, RENDER_HEIGHT);

            private static ImFontPtr CARD_FONT_RANK;
            private static ImFontPtr CARD_FONT_SUIT;

            public static void InitFonts()
            {
                unsafe
                {
                    fixed (ushort* pRange = new ushort[] { 0x20, 0x7f, 0x2660, 0x2667, 0x0 })
                    {
                        CARD_FONT_RANK = ImGui.GetIO().Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\seguisym.ttf", RENDER_WIDTH * (5.0f / 8.0f), new ImFontConfigPtr(), (nint)pRange);
                        CARD_FONT_SUIT = ImGui.GetIO().Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\seguisym.ttf", RENDER_WIDTH * (7.0f / 8.0f), new ImFontConfigPtr(), (nint)pRange);
                    }
                }
            }

            public void Render()
            {
                ImGui.BeginGroup();

                ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + RENDER_SIZE, Rgb32(1, 1, 1));

                Vector2 cursorPosTopLeft = ImGui.GetCursorPos();

                if (!FaceUp)
                {
                    ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetCursorScreenPos() + new Vector2(3, 3), ImGui.GetCursorScreenPos() + RENDER_SIZE - new Vector2(3, 3), Rgb32(0.26, 0.26, 0.26));
                }
                else if (Rank == Rank.Joker)
                {
                    using (StyleContext sc = new StyleContext())
                    {
                        sc.SetFont(CARD_FONT_SUIT);
                        sc.SetStyleColor(ImGuiCol.Text, JOKER_COLOR); // TODO color joker card bg?

                        string strJoker = "JK";
                        Vector2 strJokerSize = ImGui.CalcTextSize(strJoker);

                        ImGui.SetCursorPos(cursorPosTopLeft + RENDER_SIZE / 2 - strJokerSize / 2);
                        ImGui.TextUnformatted(strJoker);
                    }
                }
                else
                {
                    using (StyleContext sc = new StyleContext())
                    {
                        sc.SetFont(CARD_FONT_RANK);

                        string strRank = Rank switch
                        {
                            Rank._2 => "2",
                            Rank._3 => "3",
                            Rank._4 => "4",
                            Rank._5 => "5",
                            Rank._6 => "6",
                            Rank._7 => "7",
                            Rank._8 => "8",
                            Rank._9 => "9",
                            Rank._10 => "10",
                            Rank.Jack => "J",
                            Rank.Queen => "Q",
                            Rank.King => "K",
                            Rank.Ace => "A",
                            _ => throw new InvalidOperationException(),
                        };

                        Vector2 strRankSize = ImGui.CalcTextSize(strRank);

                        ImGui.SetCursorPos(cursorPosTopLeft + new Vector2(RENDER_SIZE.X / 2, RENDER_SIZE.Y * 0.25f) - strRankSize / 2);
                        ImGui.TextUnformatted(strRank);
                    }

                    using (StyleContext sc = new StyleContext())
                    {
                        sc.SetFont(CARD_FONT_SUIT);
                        sc.SetStyleColor(ImGuiCol.Text, Suit switch
                        {
                            Suit.Clubs => Rgb(0.4, 0.4, 0.4),
                            Suit.Diamonds => Rgb(1.0, 0.4, 0.4),
                            Suit.Hearts => Rgb(1.0, 0.0, 0.0),
                            Suit.Spades => Rgb(0.0, 0.0, 0.0),
                            _ => throw new InvalidOperationException(),
                        });

                        string strSuit = Suit switch
                        {
                            Suit.Clubs => "♣",
                            Suit.Diamonds => "♦",
                            Suit.Hearts => "♥",
                            Suit.Spades => "♠",
                            _ => throw new InvalidOperationException(),
                        };

                        Vector2 strSuitSize = ImGui.CalcTextSize(strSuit);

                        ImGui.SetCursorPos(cursorPosTopLeft + new Vector2(RENDER_SIZE.X / 2, RENDER_SIZE.Y * 0.70f) - strSuitSize / 2);
                        ImGui.TextUnformatted(strSuit);
                    }
                }

                ImGui.SetCursorPos(cursorPosTopLeft);
                ImGui.Dummy(RENDER_SIZE);

                ImGui.EndGroup();
            }
        }
        
        //////

        class Actor
        {
            public Actor(string name)
            {
                this.name = name;
            }

            public string name;
            public Vector3 color = new Vector3(0, 0, 0); // TODO should color be bg highlight rather than text color?
            public CardStack cstack = new CardStack();

            private Card? lastSelectedCard;

            public Card? SelectedCard
            {
                get
                {
                    if (lastSelectedCard != null && !cstack.HasCard(lastSelectedCard))
                        lastSelectedCard = null;

                    if (lastSelectedCard == null && cstack.HasAnyCards)
                        lastSelectedCard = cstack.TopCard;

                    return lastSelectedCard;
                }
                set
                {
                    lastSelectedCard = value;
                }
            }
        }

        static class UserConfig
        {
            public static float minRowHeight = 200.0f;
        }

        static StyleContext ScButtonRemove()
        {
            StyleContext sc = new StyleContext();
            sc.SetStyleColor(ImGuiCol.Button, Rgba(0, 0, 0, 0));
            sc.SetStyleColor(ImGuiCol.ButtonHovered, Rgba(1, 0, 0, 0.15));
            sc.SetStyleColor(ImGuiCol.ButtonActive, Rgba(1, 0, 0, 0.30));
            sc.SetStyleColor(ImGuiCol.Text, Rgba(0, 0, 0, 0.2));
            return sc;
        }

        static List<List<Actor>> actorRows = new List<List<Actor>>() { new List<Actor>() { } };
        static IEnumerable<Actor> Actors => actorRows.SelectMany(l => l);

        static ImFontPtr FONT_DEFAULT;
        static ImFontPtr FONT_DEFAULT_BOLD;
        static ImFontPtr FONT_ACTOR_NAMES;
        static ImFontPtr FONT_TURN_ORDER;

        static Vector4 JOKER_COLOR = Rgb(0.95, 0.55, 0.15);
        // static Vector4 JOKER_COLOR = Rgb(0.66, 0.00, 1.00);

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

            FONT_DEFAULT = io.Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\segoeui.ttf", 20);
            FONT_DEFAULT_BOLD = io.Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\segoeuib.ttf", 20);
            FONT_ACTOR_NAMES = io.Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\segoeui.ttf", 30);
            FONT_TURN_ORDER = io.Fonts.AddFontFromFileTTF(@"C:\Windows\Fonts\segoeui.ttf", 24);

            Card.InitFonts();

            CommandList cl = gd.ResourceFactory.CreateCommandList();
            ImGuiController controller = new ImGuiController(gd, gd.MainSwapchain.Framebuffer.OutputDescription, window.Width, window.Height);

            window.Resized += () =>
            {
                gd.MainSwapchain.Resize((uint)window.Width, (uint)window.Height);
                controller.WindowResized(window.Width, window.Height);
            };

            CollectAndShuffleDeck();

            Stopwatch dtStopwatch = Stopwatch.StartNew();

            // Main application loop
            while (window.Exists)
            {
                float deltaTime = (float)dtStopwatch.Elapsed.TotalSeconds;
                dtStopwatch.Restart();
                InputSnapshot snapshot = window.PumpEvents();
                if (!window.Exists) { break; }
                controller.Update(deltaTime, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

                // ImGui.ShowDemoWindow();
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

        static readonly Card[] ALL_CARDS =
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
            new Card(Suit.Joker, Rank.Joker),
            new Card(Suit.Joker, Rank.Joker),
        };

        static void CollectAndShuffleDeck()
        {
            int[] heights = Enumerable.Range(1, ALL_CARDS.Length).ToArray();
            Random.Shared.Shuffle(heights);

            for (int iCard = 0; iCard < ALL_CARDS.Length; iCard++)
            {
                ALL_CARDS[iCard].Owner = CSTACK_DECK;
                ALL_CARDS[iCard].Height = heights[iCard];
                ALL_CARDS[iCard].FaceUp = false;
            }
        }

        // TODO a way to mark an actor as out of the fight without removing them?

        static void RenderUI()
        {
            ImGuiViewportPtr viewportptr = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewportptr.WorkPos);
            ImGui.SetNextWindowSize(viewportptr.WorkSize);
            ImGui.SetNextWindowViewport(viewportptr.ID);

            using (StyleContext sc = new StyleContext())
            {
                sc.SetStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
                sc.SetStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
                sc.SetStyleVar(ImGuiStyleVar.PopupBorderSize, 0);
                sc.SetStyleVar(ImGuiStyleVar.FramePadding, new Vector2(3, 3));
                sc.SetStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0, 0));

                sc.SetStyleColor(ImGuiCol.WindowBg, Rgb(0.9, 0.9, 0.9));
                sc.SetStyleColor(ImGuiCol.PopupBg, Rgb(1.0, 1.0, 1.0));
                sc.SetStyleColor(ImGuiCol.Text, Rgb(0.0, 0.0, 0.0));
                sc.SetStyleColor(ImGuiCol.ScrollbarBg, Rgb(0.8, 0.8, 0.8));

                if (ImGui.Begin("Main", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove))
                {
                    if (ImGui.BeginTable("split", 2, ImGuiTableFlags.PadOuterX))
                    {
                        ImGui.TableSetupColumn("##left-sidebar", ImGuiTableColumnFlags.WidthFixed, 300);
                        ImGui.TableSetupColumn("##main", ImGuiTableColumnFlags.WidthStretch);

                        ImGui.TableNextColumn();
                        RenderSidebar();

                        ImGui.TableNextColumn();
                        RenderPlayingSurface();

                        ImGui.EndTable();
                    }

                }
                ImGui.End();
            }
        }

        static void RenderSidebar()
        {
            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, Rgba32(0, 0, 0, 0.1));

            using (StyleContext scTurnOrderTbl = new StyleContext())
            {
                scTurnOrderTbl.SetFont(FONT_TURN_ORDER);
                scTurnOrderTbl.SetStyleColor(ImGuiCol.TableRowBg,      Rgba(0.0, 0.0, 0.0, (Actors.Count() % 2 == 0) ? 0.00 : 0.05));
                scTurnOrderTbl.SetStyleColor(ImGuiCol.TableRowBgAlt,   Rgba(0.0, 0.0, 0.0, (Actors.Count() % 2 == 0) ? 0.05 : 0.00));

                scTurnOrderTbl.SetStyleVar(ImGuiStyleVar.CellPadding, new Vector2(8, 4));

                if (ImGui.BeginTable("##order", 2, ImGuiTableFlags.PadOuterX | ImGuiTableFlags.RowBg))
                {
                    IEnumerable<Actor> orderedActors =
                        Actors.Where(actor => !actor.cstack.HasAnyCards).Concat(
                        Actors.Where(actor => actor.SelectedCard != null && !actor.SelectedCard.FaceUp).Concat(
                        Actors.Where(actor => actor.SelectedCard != null && actor.SelectedCard.FaceUp).OrderByDescending(actor => actor.SelectedCard!.InitiativeValue)
                    ));

                    // TODO smarter sizing table columns

                    foreach (Actor actor in orderedActors)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(actor.color, actor.cstack.HasAnyCards ? 1.0f : 0.4f));

                        ImGui.TableNextRow();

                        if (actor.SelectedCard?.Rank == Rank.Joker && actor.SelectedCard!.FaceUp)
                        {
                            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.ColorConvertFloat4ToU32(new Vector4(Vector3.Lerp(JOKER_COLOR.AsVector3(), Vector3.One, 0.2f), 1.0f)));
                        }

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(actor.name);
                        ImGui.TableNextColumn();

                        // TODO more card-like display rather than text?

                        if (actor.cstack.HasAnyCards)
                        {
                            ImGui.TextUnformatted(actor.SelectedCard!.FaceUp ? actor.SelectedCard.ToString() : "????????");
                        }

                        ImGui.PopStyleColor();
                    }
                
                    ImGui.EndTable();
                }
            }

            using (StyleContext scStacksAndButtons = new StyleContext())
            {
                const float spaceBetweenButtons = 4;
                const float dYSpaceForButtons = 150;

                scStacksAndButtons.SetStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(spaceBetweenButtons, spaceBetweenButtons));
                scStacksAndButtons.SetStyleVar(ImGuiStyleVar.CellPadding, new Vector2(spaceBetweenButtons / 2, spaceBetweenButtons / 2));

                if (ImGui.BeginChild("deckandbtns", new Vector2(0, 0), ImGuiChildFlags.AlwaysUseWindowPadding))
                {
                    Vector2 cursorPosRenderButtons = ImGui.GetCursorPos() + new Vector2(0, ImGui.GetContentRegionAvail().Y - dYSpaceForButtons);
                    {
                        Vector2 cursorPosRenderStacks = cursorPosRenderButtons - new Vector2(0, ImGui.GetStyle().ItemSpacing.Y);
                        CSTACK_DECK.Render(cursorPosRenderStacks);
                        CSTACK_DISCARD.Render(cursorPosRenderStacks + new Vector2(Card.RENDER_WIDTH + ImGui.GetStyle().ItemSpacing.X, 0));
                    }
                    ImGui.SetCursorPos(cursorPosRenderButtons);

                    using (StyleContext scButtons = new StyleContext())
                    {
                        scButtons.SetStyleColor(ImGuiCol.Button, Rgba(0, 0, 0, 0.26));
                        scButtons.SetStyleColor(ImGuiCol.Text, Rgb(1, 1, 1));

                        Vector2 btnSize = new Vector2(
                            (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().CellPadding.Y * 4) / 3,
                            (ImGui.GetContentRegionAvail().Y - ImGui.GetStyle().CellPadding.Y * 4) / 2);

                        if (ImGui.BeginTable("##buttons", 3))
                        {
                            ImGui.TableNextColumn();

                            bool notEnoughCardsToDeal = Actors.Count() > CSTACK_DECK.Cards.Count();

                            ImGui.BeginDisabled(!Actors.Any() || notEnoughCardsToDeal);
                            if (ImGui.Button("deal", btnSize))
                            {
                                foreach (Actor actor in Actors)
                                {
                                    foreach (Card card in actor.cstack.Cards.ToArray())
                                    {
                                        card.Discard();
                                    }
                                }

                                foreach (Actor actor in Actors)
                                {
                                    CSTACK_DECK.TopCard!.Owner = actor.cstack;
                                }
                            }
                            ImGui.EndDisabled();

                            if (notEnoughCardsToDeal && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, Rgb(0, 0, 0));
                                ImGui.SetTooltip("There are not enough cards remaining in the deck to give every player a card.\nYou should reshuffle the deck first.");
                                ImGui.PopStyleColor();
                            }

                            ImGui.TableNextColumn();

                            ImGui.BeginDisabled(Actors.All(actor => !actor.cstack.HasAnyCards));
                            if (ImGui.Button("reveal", btnSize))
                            {
                                foreach (Actor actor in Actors)
                                {
                                    foreach (Card card in actor.cstack.Cards.ToArray())
                                    {
                                        card.FaceUp = true;
                                    }
                                }
                            }
                            ImGui.EndDisabled();

                            ImGui.TableNextColumn();

                            if (ImGui.Button("reshuffle", btnSize))
                            {
                                CollectAndShuffleDeck();
                            }

                            ImGui.TableNextColumn();

                            // TODO
                            // ImGui.Button("undo", btnSize);

                            ImGui.TableNextColumn();

                            if (ImGui.Button("actors...", btnSize))
                                ImGui.OpenPopup(actorCfgPopupName);

                            TryRenderActorCfgPopup();

                            ImGui.TableNextColumn();

                            // TODO
                            // ImGui.Button("settings...", btnSize);

                            ImGui.EndTable();
                        }
                    }
                }
                ImGui.EndChild();
            }
        }

        static string actorCfgPopupName = "actorcfg";

        static void TryRenderActorCfgPopup()
        {
            using (StyleContext scPopup = new StyleContext())
            {
                scPopup.SetStyleColor(ImGuiCol.Text, Rgb(0, 0, 0));

                // Precalculate popup size to position it.
                // (This is probably one of the biggest downsides of using Dear ImGui)

                float popupHeight = 0;

                {
                    float separatorHeight = (ImGui.GetStyle().ItemSpacing.Y * 2);
                    float rowHeight = ImGui.GetFrameHeight() + ImGui.GetStyle().CellPadding.Y * 2;

                    popupHeight += ImGui.GetStyle().WindowPadding.Y * 2; // padding
                    popupHeight += actorRows.Count * separatorHeight; // separators
                    popupHeight += actorRows.Count * (rowHeight * 2); // header + add button
                    popupHeight += Actors.Count() * rowHeight; // actor rows
                    popupHeight += rowHeight; // final add button
                }

                if (ImGui.IsPopupOpen(actorCfgPopupName))
                {
                    ImGui.SetNextWindowPos(new Vector2(ImGui.GetItemRectMin().X, ImGui.GetItemRectMin().Y - popupHeight - ImGui.GetStyle().ItemSpacing.Y));
                    ImGui.SetNextWindowSize(new Vector2(0, popupHeight));
                }

                if (ImGui.BeginPopup(actorCfgPopupName))
                {
                    List<Actor>? rowRemove = null;

                    for (int iRow = 0; iRow < actorRows.Count; iRow++)
                    {
                        ImGui.PushID(iRow);

                        List<Actor> row = actorRows[iRow];

                        Actor? actorRemove = null;
                        bool addNewActor = false;

                        // TODO color selectors pop up window in weird spot

                        if (ImGui.BeginTable("##actors", 3))
                        {
                            {
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();

                                Vector3 colorRow = row.Select(actor => actor.color).Distinct().Count() == 1 ? row[0].color : new Vector3(0, 0, 0);
                                if (ImGui.ColorEdit3("##rowcolor", ref colorRow, ImGuiColorEditFlags.NoInputs))
                                {
                                    foreach (Actor actor in row)
                                    {
                                        actor.color = colorRow;
                                    }
                                }

                                ImGui.TableNextColumn();

                                ImGui.AlignTextToFramePadding();
                                ImGui.PushFont(FONT_DEFAULT_BOLD);
                                ImGui.Text($"Row {iRow + 1}");
                                ImGui.PopFont();

                                ImGui.TableNextColumn();

                                using (StyleContext sc = ScButtonRemove())
                                {
                                    if (actorRows.Count > 1 && ImGui.Button("X", new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight())))
                                        rowRemove = row;
                                }
                            }

                            for (int iActor = 0; iActor < row.Count; iActor++)
                            {
                                ImGui.PushID(iActor);

                                Actor actor = row[iActor];

                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();

                                ImGui.ColorEdit3("##color", ref actor.color, ImGuiColorEditFlags.NoInputs);

                                ImGui.TableNextColumn();

                                using (StyleContext sc = new StyleContext())
                                {
                                    sc.SetStyleColor(ImGuiCol.FrameBg, Rgba(0, 0, 0, 0));
                                    sc.SetStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0, ImGui.GetStyle().FramePadding.Y));

                                    float nameWidth = ImGui.CalcTextSize(actor.name).X + ImGui.GetStyle().FramePadding.X * 2;
                                    ImGui.SetNextItemWidth(Math.Max(40, nameWidth));
                                    ImGui.InputText("##name", ref actor.name, (uint)actor.name.Length + 1024, ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.AutoSelectAll);
                                }

                                ImGui.TableNextColumn();

                                using (StyleContext sc = ScButtonRemove())
                                {
                                    if (ImGui.Button("X", new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight())))
                                        actorRemove = actor;
                                }

                                ImGui.PopID();
                            }

                            ImGui.TableNextRow();

                            ImGui.TableNextColumn();

                            if (ImGui.Selectable("+##plusactor", false, ImGuiSelectableFlags.NoAutoClosePopups | ImGuiSelectableFlags.SpanAllColumns, new Vector2(0, ImGui.GetFrameHeight())))
                                addNewActor = true;

                            ImGui.EndTable();
                        }

                        if (actorRemove != null)
                        {
                            foreach (Card card in actorRemove.cstack.Cards)
                            {
                                card.Discard();
                            }

                            row.Remove(actorRemove);
                        }

                        if (addNewActor)
                        {
                            row.Add(new Actor("New actor"));
                        }

                        ImGui.PopID();
                        ImGui.Separator();
                    }

                    if (ImGui.BeginTable("newrow", 1))
                    {
                        ImGui.TableNextColumn();

                        if (ImGui.Selectable("+ new row", false, ImGuiSelectableFlags.NoAutoClosePopups | ImGuiSelectableFlags.SpanAllColumns, new Vector2(0, ImGui.GetFrameHeight())))
                        {
                            actorRows.Add(new List<Actor>() { });
                        }

                        ImGui.EndTable();
                    }

                    if (rowRemove != null)
                    {
                        foreach (Actor actor in rowRemove)
                        {
                            foreach (Card card in actor.cstack.Cards)
                            {
                                card.Discard();
                            }
                        }

                        actorRows.Remove(rowRemove);
                    }

                    if (actorRows.Count == 0)
                    {
                        actorRows.Add(new List<Actor>());
                    }

                    ImGui.EndPopup();
                }
            }
        }

        static void RenderPlayingSurface()
        {
            using (StyleContext scPlayingSfc = new StyleContext())
            {
                scPlayingSfc.SetStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(16, 12));

                if (ImGui.BeginChild("playingsfc", new Vector2(0, 0), ImGuiChildFlags.AlwaysUseWindowPadding))
                {
                    float lastRowY = ImGui.GetCursorPosY();

                    for(int iRow = 0; iRow < actorRows.Count; iRow++)
                    {
                        if (iRow > 0)
                        {
                            if (ImGui.GetCursorPosY() < (lastRowY + UserConfig.minRowHeight))
                            {
                                ImGui.Dummy(new Vector2(0, (lastRowY + UserConfig.minRowHeight) - ImGui.GetCursorPosY()));
                            }
                        }

                        // TODO center each row?

                        for (int iActor = 0; iActor < actorRows[iRow].Count; iActor++)
                        {
                            if (iActor > 0)
                            {
                                ImGui.SameLine();
                            }

                            Actor actor = actorRows[iRow][iActor];

                            ImGui.PushID(iRow);
                            ImGui.PushID(iActor);

                            int nCards = actor.cstack.Cards.Count();

                            // TODO this logic is weird

                            float nameWidth;
                            float cardsWidth;
                            float regionWidth;
                            {
                                ImGuiStylePtr imStyle = ImGui.GetStyle();

                                ImGui.PushFont(FONT_ACTOR_NAMES);
                                nameWidth = ImGui.CalcTextSize(actor.name).X + imStyle.FramePadding.X * 2;
                                ImGui.PopFont();

                                cardsWidth = (nCards * Card.RENDER_WIDTH) + ((nCards - 1) * imStyle.ItemSpacing.X);

                                float minWidth = (2.5f * Card.RENDER_WIDTH) + (2 * imStyle.ItemSpacing.X);

                                regionWidth = Math.Max(minWidth, Math.Max(nameWidth, cardsWidth));
                            }

                            float actorGroupStartX = ImGui.GetCursorPosX();

                            ImGui.BeginGroup();
                            {
                                using (StyleContext sc = new StyleContext())
                                {
                                    sc.SetStyleColor(ImGuiCol.FrameBg, Rgba(0, 0, 0, 0));
                                    sc.SetFont(FONT_ACTOR_NAMES);
                                    sc.SetStyleColor(ImGuiCol.Text, new Vector4(actor.color, 1.0f));

                                    ImGui.SetCursorPosX(actorGroupStartX + ((regionWidth / 2) - (nameWidth / 2)));
                                    ImGui.SetNextItemWidth(nameWidth);
                                    ImGui.InputText($"##name", ref actor.name, (uint)actor.name.Length + 1024, ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.AutoSelectAll);
                                }

                                ImGui.SetCursorPosX(actorGroupStartX + ((regionWidth / 2) - (cardsWidth / 2)));

                                int idCard = 1;
                                foreach (Card card in actor.cstack.CardsBestToWorst)
                                {
                                    ImGui.PushID(idCard++);
                                    ImGui.BeginGroup();

                                    card.Render();

                                    if (ImGui.IsItemClicked())
                                    {
                                        if (!card.FaceUp)
                                        {
                                            card.FaceUp = true;
                                        }
                                        else
                                        {
                                            actor.SelectedCard = card;
                                        }
                                    }

                                    if (card.FaceUp && card != actor.SelectedCard)
                                    {
                                        ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), Rgba32(1, 1, 1, 0.93f));
                                    }

                                    using (StyleContext sc = ScButtonRemove())
                                    {
                                        if (ImGui.Button("discard", new Vector2(ImGui.GetItemRectSize().X, 0)))
                                            card.Discard();
                                    }

                                    ImGui.EndGroup();
                                    ImGui.PopID();

                                    ImGui.SameLine();
                                }

                                if (CSTACK_DECK.HasAnyCards)
                                {
                                    using (StyleContext sc = new StyleContext())
                                    {
                                        sc.SetFont(FONT_ACTOR_NAMES);
                                        sc.SetStyleColor(ImGuiCol.Button, Rgba(0, 0, 0, 0));
                                        sc.SetStyleColor(ImGuiCol.Text, Rgba(0, 0, 0, 0.2));

                                        if (nCards == 0)
                                        {
                                            ImGui.SetCursorPosX(actorGroupStartX + ((regionWidth / 2) - (Card.RENDER_WIDTH / 2)));
                                        }

                                        if (ImGui.Button("+", new Vector2(nCards == 0 ? Card.RENDER_WIDTH : 30, Card.RENDER_HEIGHT)))
                                        {
                                            Card newcard = CSTACK_DECK.TopCard!;
                                            newcard.Owner = actor.cstack;
                                            newcard.FaceUp = true;
                                        }
                                    }
                                }
                            }

                            ImGui.SetCursorPosX(actorGroupStartX);
                            ImGui.Dummy(new Vector2(regionWidth, 0));

                            ImGui.EndGroup();

                            ImGui.PopID();
                            ImGui.PopID();
                        }
                    }
                }
                ImGui.EndChild();
            }
        }

        static Vector4 Rgba(double r, double g, double b, double a)
        {
            return new Vector4((float)r, (float)g, (float)b, (float)a);
        }

        static Vector4 Rgb(double r, double g, double b)
        {
            return Rgba(r, g, b, 1.0);
        }

        static uint Rgba32(double r, double g, double b, double a)
        {
            return ImGui.ColorConvertFloat4ToU32(Rgba(r, g, b, a));
        }

        static uint Rgb32(double r, double g, double b)
        {
            return ImGui.ColorConvertFloat4ToU32(Rgb(r, g, b));
        }
    }
}
