// This file is part of Mystery Dungeon eXtended.

// Copyright (C) 2015 Pikablu, MDX Contributors, PMU Staff

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Affero General Public License for more details.

// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Server;
using Server.Network;
using Server.Players;
using Server.Stories;
using System.Xml;
using System.IO;

namespace Script {
    public class Lottery {
        public const DayOfWeek LOTTERY_DAY = DayOfWeek.Friday;

        public static int LotteryPayout = 0;
        public static int LotteryEarnings = 0;
        public static int LastLotteryEarnings = 0;
        public static int TotalLotteryEarnings = 0;
        public static int[] LotteryCurrentNumbers = new int[6];
        public static DateTime? LotteryLastDraw;

        public static void LoadLottery() {
            using (XmlReader reader = XmlReader.Create(Path.Combine(Server.IO.Paths.ScriptsIOFolder, "lottery.xml"))) {
                while (reader.Read()) {
                    if (reader.IsStartElement()) {
                        switch (reader.Name) {
                            case "Payout": {
                                    LotteryPayout = reader.ReadString().ToInt();
                                }
                                break;
                            case "Earnings": {
                                    LotteryEarnings = reader.ReadString().ToInt();
                                }
                                break;
                            case "LastEarnings": {
                                    LastLotteryEarnings = reader.ReadString().ToInt();
                                }
                                break;
                            case "TotalEarnings": {
                                    TotalLotteryEarnings = reader.ReadString().ToInt();
                                }
                                break;
                            case "LastDrawDate": {
                                    LotteryLastDraw = reader.ReadString().ToDate();
                                }
                                break;
                            case "DrawNum": {
                                    LotteryCurrentNumbers[reader["num"].ToInt()] = reader.ReadString().ToInt();
                                }
                                break;
                        }
                    }
                }
            }
        }

        public static void SaveLottery() {
            using (XmlWriter writer = XmlWriter.Create(Path.Combine(Server.IO.Paths.ScriptsIOFolder, "lottery.xml"), Settings.XmlWriterSettings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Lottery");

                writer.WriteStartElement("Data");
                writer.WriteElementString("Payout", LotteryPayout.ToString());
                writer.WriteElementString("Earnings", LotteryEarnings.ToString());
                writer.WriteElementString("LastEarnings", LastLotteryEarnings.ToString());
                writer.WriteElementString("TotalEarnings", TotalLotteryEarnings.ToString());
                writer.WriteElementString("LastDrawDate", LotteryLastDraw.HasValue ? LotteryLastDraw.ToString() : "null");
                for (int i = 0; i < 6; i++) {
                    writer.WriteStartElement("DrawNum");
                    writer.WriteAttributeString("num", i.ToString());
                    writer.WriteString(LotteryCurrentNumbers[i].ToString());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        public static void VerifyWinner(Client client) {
            if (client.Player.IsInvFull()) {
                Messenger.PlayerMsg(client, "Your inventory is full. Come back when you have more room!", Text.BrightRed);
            } else if (client.Player.HasItem(381) > 0 && DateTime.Now.DayOfWeek == LOTTERY_DAY) {
                client.Player.TakeItem(381, 1);
                GenLottoNumbers();
                int[] playerNum = new int[6];
                int matchCount = 0;
                for (int i = 0; i < 6; i++) {
                    // *A 1-9 range has a 0.0028038025 chance to give 4 matches.
                    playerNum[i] = Server.Math.Rand(1, 10);
                    if (playerNum[i] == LotteryCurrentNumbers[i]) {
                        matchCount++;
                    }
                }

                string lottoStr = LotteryCurrentNumbers[0].ToString();
                string playerStr = playerNum[0].ToString();
                for (int i = 1; i < 6; i++) {
                    lottoStr += ", " + LotteryCurrentNumbers[i].ToString();
                    playerStr += ", " + playerNum[i].ToString();
                }
                Messenger.PlayerMsg(client, "Your numbers are: " + playerStr, Text.Green);
                Messenger.PlayerMsg(client, "The winning numbers are: " + lottoStr, Text.Green);
                switch (matchCount) {
                    case 7: {// All 7 digits matched
                            client.Player.GiveItem(1, (LastLotteryEarnings / 2));
                            Messenger.PlayerMsg(client, "All numbers matched! You won " + (LastLotteryEarnings / 2).ToString() + " Poké!", Text.Yellow);
                            LotteryPayout += (LastLotteryEarnings / 2);
                            SaveLottery();
                            Messenger.GlobalMsg("[Lottery] " + client.Player.Name + " has matched all 7 numbers in the lottery!", Text.Yellow);
                        }
                        break;
                    case 6: {
                            client.Player.GiveItem(437, 1);
                            Messenger.PlayerMsg(client, "6 numbers matched! You won 1 Ice Rock!", Text.Yellow);
                            Messenger.GlobalMsg("[Lottery] " + client.Player.Name + " has matched 6 numbers in the lottery!", Text.Yellow);
                        }
                        break;
                    case 5: {
                            client.Player.GiveItem(143, 1);
                            Messenger.PlayerMsg(client, "5 numbers matched! You won 1 Life Orb!", Text.Yellow);
                            Messenger.GlobalMsg("[Lottery] " + client.Player.Name + " has matched 5 numbers in the lottery!", Text.Yellow);
                        }
                        break;
                    case 4: {
                            client.Player.GiveItem(1, 800);
                            Messenger.PlayerMsg(client, "4 numbers matched! You won 800 Poké!", Text.Yellow);
                            LotteryPayout += 800;
                            SaveLottery();
                            //NetScript.GlobalMsg("[Lottery] " + NetScript.GetPlayerName(index) + " has matched 4 numbers in the lottery!", Text.Yellow);
                        }
                        break;
                    case 3: {
                            client.Player.GiveItem(8, 5);
                            Messenger.PlayerMsg(client, "3 numbers matched! You won 5 Sitrus Berries!", Text.Yellow);
                        }
                        break;
                    case 2: {
                            client.Player.GiveItem(1, 100);
                            Messenger.PlayerMsg(client, "2 numbers matched! You won 100 Poké!", Text.Yellow);
                            LotteryPayout += 100;
                            SaveLottery();
                        }
                        break;
                    case 1: {
                            client.Player.GiveItem(1, 25);
                            Messenger.PlayerMsg(client, "1 number matched! You won 25 Poké!", Text.Yellow);
                            LotteryPayout += 25;
                            SaveLottery();
                        }
                        break;
                    case 0: {
                            Messenger.PlayerMsg(client, "No numbers matched! You didn't win anything...", Text.Yellow);
                        }
                        break;
                }
            }
        }

        public static void BuyLotteryTicket(Client client) {
            int price = 25;
            if (client.Player.IsInvFull() == false || client.Player.HasItem(381) > 0) {
                LotteryEarnings += price;
                TotalLotteryEarnings += price;
                client.Player.TakeItem(1, price);
                client.Player.GiveItem(381, 1);
                Messenger.PlayerMsg(client, "You have purchased one lottery ticket!  Come back on " + LOTTERY_DAY.ToString() + " to see if you've won!", Text.Yellow);
                //SaveLottery();
            } else {
                Messenger.PlayerMsg(client, "Your inventory is full.", Text.BrightRed);
            }
        }

        public static void ForceGenLottoNumbers() {
            LotteryLastDraw = DateTime.Now;
            for (int i = 0; i < 6; i++) {
                LotteryCurrentNumbers[i] = Server.Math.Rand(1, 10);
            }
            LastLotteryEarnings = LotteryEarnings;
            LotteryEarnings = 0;
            LotteryPayout = 0;
            SaveLottery();
        }

        public static void GenLottoNumbers() {
            if (ShouldGenLottoNumbers()) {
                ForceGenLottoNumbers();
            }
        }

        public static bool ShouldGenLottoNumbers() {
            if (LotteryLastDraw != null) {
                if (DateTime.Today.Subtract(LotteryLastDraw.Value).Days >= 7 && DateTime.Now.DayOfWeek == LOTTERY_DAY) {
                    return true;
                } else {
                    return false;
                }
            } else {
                return true;
            }
        }

    }
}
