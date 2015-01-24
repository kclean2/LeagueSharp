﻿using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace Sharpshooter.Champions
{
    public static class Twitch
    {
        static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        static Orbwalking.Orbwalker Orbwalker { get { return SharpShooter.Orbwalker; } }

        static Spell Q, W, E, Recall;

        public static void Load()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 950);
            E = new Spell(SpellSlot.E, 1200);
            Recall = new Spell(SpellSlot.Recall);

            W.SetSkillshot(0.25f, 120f, 1750f, false, SkillshotType.SkillshotCircle);

            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseW", "Use W", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseE", "Use E", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Combo").AddItem(new MenuItem("comboUseEStack", "Cast E if Stack >=", true).SetValue(new Slider(6, 1, 6)));

            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassUseW", "Use W", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassUseE", "Use E", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassUseEStack", "Cast E if Stack >=", true).SetValue(new Slider(4, 1, 6)));
            SharpShooter.Menu.SubMenu("Harass").AddItem(new MenuItem("harassMana", "If Mana % >", true).SetValue(new Slider(50, 0, 100)));

            SharpShooter.Menu.SubMenu("Laneclear").AddItem(new MenuItem("laneclearUseW", "Use W", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Laneclear").AddItem(new MenuItem("laneclearUseE", "Use E", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Laneclear").AddItem(new MenuItem("laneclearMana", "if Mana % >", true).SetValue(new Slider(50, 0, 100)));

            SharpShooter.Menu.SubMenu("Jungleclear").AddItem(new MenuItem("jungleclearUseW", "Use W", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Jungleclear").AddItem(new MenuItem("jungleclearUseE", "Use E", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Jungleclear").AddItem(new MenuItem("jungleclearMana", "if Mana % >", true).SetValue(new Slider(20, 0, 100)));

            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("killsteal", "Use Killsteal", true).SetValue(true));
            SharpShooter.Menu.SubMenu("Misc").AddItem(new MenuItem("stealthrecall", "Stealth Recall", true).SetValue(new KeyBind('T',KeyBindType.Press)));

            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingAA", "Real AA Range", true).SetValue(new Circle(true, Color.LightGreen)));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingW", "W Range", true).SetValue(new Circle(true, Color.LightGreen)));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingE", "E Range", true).SetValue(new Circle(false, Color.LightGreen)));
            SharpShooter.Menu.SubMenu("Drawings").AddItem(new MenuItem("drawingQtimer", "Stealth Timer", true).SetValue(true));

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                Harass();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                Laneclear();
                Jungleclear();
            }

            if (SharpShooter.Menu.Item("stealthrecall", true).GetValue<KeyBind>().Active)
            {
                if (Q.IsReady() && Recall.IsReady())
                {
                    Q.Cast();
                    Recall.Cast();
                }
            }

            Killsteal();
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            var drawingAA = SharpShooter.Menu.Item("drawingAA", true).GetValue<Circle>();
            var drawingW = SharpShooter.Menu.Item("drawingW", true).GetValue<Circle>();
            var drawingE = SharpShooter.Menu.Item("drawingE", true).GetValue<Circle>();

            if (drawingAA.Active)
                Render.Circle.DrawCircle(Player.Position, Orbwalking.GetRealAutoAttackRange(Player), drawingAA.Color);

            if (W.IsReady() && drawingW.Active)
                Render.Circle.DrawCircle(Player.Position, W.Range, drawingW.Color);

            if (drawingE.Active)
                Render.Circle.DrawCircle(Player.Position, E.Range, drawingE.Color);

            if(SharpShooter.Menu.Item("drawingQtimer", true).GetValue<Boolean>())
            {
                foreach (var buff in Player.Buffs)
                {
                    if (buff.Name == "TwitchHideInShadows")
                    {
                        var targetpos = Drawing.WorldToScreen(Player.Position);
                        Drawing.DrawText(targetpos[0] - 10, targetpos[1], Color.Gold, "" + (buff.EndTime - Game.ClockTime));
                    }
                }
            }

            if (SharpShooter.Menu.Item("stealthrecall", true).GetValue<KeyBind>().Active)
            {
                var targetpos = Drawing.WorldToScreen(Player.Position);

                if (Q.IsReady() && Recall.IsReady())
                {
                    Drawing.DrawText(targetpos[0] - 60, targetpos[1] - 50, Color.Gold, "Try Stealth recall");
                }
                else if (Player.HasBuff("TwitchHideInShadows") && Player.HasBuff("Recall"))
                    Drawing.DrawText(targetpos[0] - 60, targetpos[1] - 50, Color.Gold, "Stealth recall Activated");
                else if (!Player.HasBuff("recall"))
                    Drawing.DrawText(targetpos[0] - 60, targetpos[1] - 50, Color.Gold, "Q is not ready");
            }
        }

        static void Killsteal()
        {
            if (!SharpShooter.Menu.Item("killsteal", true).GetValue<Boolean>())
                return;

            foreach (Obj_AI_Hero target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(E.Range) && x.IsEnemy && !x.IsDead && !x.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (target != null)
                {
                    if (E.CanCast(target) && (target.Health + target.HPRegenRate) <= E.GetDamage(target))
                        E.Cast();
                }
            }
        }

        static void Combo()
        {
            if (!Orbwalking.CanMove(1))
                return;

            var Wtarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.True, false);
            var Etarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical, true);

            if (W.CanCast(Wtarget) && W.GetPrediction(Wtarget).Hitchance >= HitChance.High && SharpShooter.Menu.Item("comboUseW", true).GetValue<Boolean>())
                W.Cast(Wtarget);

            if (E.CanCast(Etarget) && SharpShooter.Menu.Item("comboUseE", true).GetValue<Boolean>())
            {
                foreach (var buff in Etarget.Buffs)
                {
                    if(buff.Name == "twitchdeadlyvenom")
                    {
                        if (buff.Count >= SharpShooter.Menu.Item("comboUseEStack", true).GetValue<Slider>().Value)
                        {
                            E.Cast();
                            break;
                        }
                    }
                }
            }
                
        }

        static void Harass()
        {
            if (!Orbwalking.CanMove(1) || !(Player.ManaPercentage() > SharpShooter.Menu.Item("harassMana", true).GetValue<Slider>().Value))
                return;

            var Wtarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.True, false);
            var Etarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical, true);

            if (W.CanCast(Wtarget) && W.GetPrediction(Wtarget).Hitchance >= HitChance.High && SharpShooter.Menu.Item("harassUseW", true).GetValue<Boolean>())
                W.Cast(Wtarget);

            if (E.CanCast(Etarget) && SharpShooter.Menu.Item("harassUseE", true).GetValue<Boolean>())
            {
                foreach (var buff in Etarget.Buffs)
                {
                    if (buff.Name == "twitchdeadlyvenom")
                    {
                        if (buff.Count >= SharpShooter.Menu.Item("harassUseEStack", true).GetValue<Slider>().Value)
                        {
                            E.Cast();
                            break;
                        }
                    }
                }
            }
        }

        static void Laneclear()
        {
            if (!Orbwalking.CanMove(1) || !(Player.ManaPercentage() > SharpShooter.Menu.Item("laneclearMana", true).GetValue<Slider>().Value))
                return;

            var Minions = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Enemy);

            if (Minions.Count <= 0)
                return;

            if (W.IsReady() && SharpShooter.Menu.Item("laneclearUseW", true).GetValue<Boolean>())
            {
                var Farmloc = W.GetCircularFarmLocation(Minions);

                if (Farmloc.MinionsHit >= 3)
                    W.Cast(Farmloc.Position);
            }

            if (E.IsReady() && SharpShooter.Menu.Item("laneclearUseE", true).GetValue<Boolean>())
            {
                foreach (var Minion in Minions)
                {
                    foreach (var buff in Minion.Buffs)
                    {
                        if (buff.Name == "twitchdeadlyvenom")
                        {
                            if (buff.Count >= 6)
                            {
                                E.Cast();
                                break;
                            }
                        }
                    }

                    if (Minion.Health <= E.GetDamage(Minion))
                        E.Cast();
                }
            }
        }

        static void Jungleclear()
        {
            if (!Orbwalking.CanMove(1) || !(Player.ManaPercentage() > SharpShooter.Menu.Item("jungleclearMana", true).GetValue<Slider>().Value))
                return;

            var Mobs = MinionManager.GetMinions(Player.ServerPosition, Orbwalking.GetRealAutoAttackRange(Player) + 100, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (Mobs.Count < 1)
                return;

            if (W.CanCast(Mobs[0]) && SharpShooter.Menu.Item("jungleclearUseW", true).GetValue<Boolean>())
                W.Cast(Mobs[0].Position);

            if (E.CanCast(Mobs[0]) && SharpShooter.Menu.Item("jungleclearUseE", true).GetValue<Boolean>())
            {
                foreach (var buff in Mobs[0].Buffs)
                {
                    if (buff.Name == "twitchdeadlyvenom")
                    {
                        if (buff.Count >= 6)
                        {
                            E.Cast();
                            break;
                        }
                    }
                }

                if ((Mobs[0].Health + Mobs[0].HPRegenRate) <= E.GetDamage(Mobs[0]))
                    E.Cast();
            }
        }
    }
}
