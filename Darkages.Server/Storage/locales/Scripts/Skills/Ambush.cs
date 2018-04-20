﻿///************************************************************************
//Project Lorule: A Dark Ages Server (http://darkages.creatorlink.net/index/)
//Copyright(C) 2018 TrippyInc Pty Ltd
//
//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.
//
//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//GNU General Public License for more details.
//
//You should have received a copy of the GNU General Public License
//along with this program.If not, see<http://www.gnu.org/licenses/>.
//*************************************************************************/
using Darkages.Network.ServerFormats;
using Darkages.Scripting;
using Darkages.Types;
using System;
using System.Linq;

namespace Darkages.Storage.locales.Scripts.Skills
{
    [Script("Ambush", "Dean")]
    public class Ambush : SkillScript
    {
        public Skill _skill;

        public Random rand = new Random();

        public Ambush(Skill skill) : base(skill)
        {
            _skill = skill;
        }

        public override void OnFailed(Sprite sprite)
        {
            if (sprite is Aisling)
            {
                var client = (sprite as Aisling).Client;

                client.SendMessage(0x02,
                    string.IsNullOrEmpty(Skill.Template.FailMessage) ? Skill.Template.FailMessage : "failed.");
            }
        }

        public override void OnSuccess(Sprite sprite)
        {
            if (sprite is Aisling)
            {
                var client = (sprite as Aisling).Client;

                var targets = client.Aisling.GetInfront(3, true).ToList();
                var prev = client.Aisling.Position;
                Position targetPosition = null;

                if (targets != null && targets.Count > 0)
                    foreach (var target in targets)
                    {
                        if (target == null)
                            continue;

                        if (target is Money)
                            continue;

                        if (target is Item)
                            continue;

                        if (target.Serial == client.Aisling.Serial)
                            continue;

                        var blocks = target.Position.SurroundingContent(client.Aisling.Map);


                        if (blocks.Length > 0)
                        {
                            var selections = blocks.Where(i => i.Content ==
                                                               TileContent.Item
                                                               || i.Content == TileContent.Money
                                                               || i.Content == TileContent.None).ToArray();
                            var selection = selections
                                .OrderByDescending(i => i.Position.DistanceFrom(client.Aisling.Position))
                                .FirstOrDefault();
                            if (selections.Length == 0 || selection == null)
                            {
                                client.SendMessageBox(0x02, ServerContext.Config.CantDoThat);
                                return;
                            }

                            targetPosition = selection.Position;
                        }


                        if (targetPosition != null)
                        {
                            client.Aisling.X = targetPosition.X;
                            client.Aisling.Y = targetPosition.Y;


                            if (!client.Aisling.Facing(target.X, target.Y, out var direction))
                            {
                                client.Aisling.Direction = (byte)direction;

                                if (client.Aisling.Position.IsNextTo(target.Position))
                                    client.Aisling.Turn();
                            }

                            client.Aisling.Map.Update(prev.X, prev.Y, TileContent.None);
                            client.Aisling.Map.Update(client.Aisling.X, client.Aisling.Y, TileContent.Aisling);

                            client.Refresh();
                            return;
                        }
                    }
            }
        }

        public override void OnUse(Sprite sprite)
        {
            if (sprite is Aisling)
            {
                var client = (sprite as Aisling).Client;
                if (client.Aisling != null && !client.Aisling.Dead && Skill.Ready)
                {
                    client.TrainSkill(Skill);

                    var success = Skill.RollDice(rand);

                    if (success)
                        OnSuccess(sprite);
                    else
                        OnFailed(sprite);
                }
            }
        }
    }
}
