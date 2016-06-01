using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using Color = System.Drawing.Color;

namespace Bristana
{
    class Program
    {
        public static Spell.Active Q;
        public static Spell.Skillshot W;
        public static Spell.Targeted E;
        public static Spell.Targeted R;
        
        public static Menu Menu,
        SpellMenu,
        JungleMenu,
        HarassMenu,
        LaneMenu,
        StealMenu,
        Misc,
        Skin;

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoadingComplete;
        }

        public static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }
        }

        private static void Game_OnTick(EventArgs args)
        {
             if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
             {
                 JungleClear();
             }
             if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
             {
                 Flee();
             }
             if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
             {
                 Combo();
             }
             if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
             {
                 Harass();
             }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LaneClear();
            }
            KillSteal();
			
            if (_Player.SkinId != Skin["skin.Id"].Cast<ComboBox>().CurrentValue)
            {
                if (checkSkin())
                {
                    Player.SetSkinId(SkinId());
                }
            }

        }
		
        private static void JungleClear()
        {
            var source =
                EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, Q.Range)
                    .OrderByDescending(a => a.MaxHealth)
                    .FirstOrDefault();
            if (source == null) return;
            if (Q.IsReady() && JungleMenu["jungleQ"].Cast<CheckBox>().CurrentValue && source.Distance(_Player) <= Q.Range)
            {
                Q.Cast();
            }
            if (W.IsReady() && JungleMenu["jungleW"].Cast<CheckBox>().CurrentValue && source.Distance(_Player) <= W.Range)
            {
                W.Cast(source.ServerPosition);
            }
            if (E.IsReady() && JungleMenu["jungleE"].Cast<CheckBox>().CurrentValue && source.Distance(_Player) <= E.Range && _Player.ManaPercent > JungleMenu["manaJung"].Cast<Slider>().CurrentValue)
            {
                E.Cast(source);
            }
        }
		
        private static void Flee()
        {
            if (W.IsReady())
            {
                var cursorPos = Game.CursorPos;
                var castPos = Player.Instance.Position.Distance(cursorPos) <= W.Range ? cursorPos : Player.Instance.Position.Extend(cursorPos, W.Range).To3D();
                W.Cast(castPos);
			}
		}
		
        public static int SkinId()
        {
            return Skin["skin.Id"].Cast<ComboBox>().CurrentValue;
        }
        public static bool checkSkin()
        {
            return Skin["checkSkin"].Cast<CheckBox>().CurrentValue;
        }

        private static void Gapcloser_OnGapCloser(Obj_AI_Base sender, Gapcloser.GapcloserEventArgs args)
        {
            if(Misc["antiGap"].Cast<CheckBox>().CurrentValue && args.Sender.Distance(_Player)<300)
            {
                R.Cast(args.Sender);
            }
        }

        private static void Harass()
        {
            var enemies = EntityManager.Heroes.Enemies.OrderByDescending
                (a => a.HealthPercent).Where(a => !a.IsMe && a.IsValidTarget() && a.Distance(_Player) <= E.Range);
            var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
            if (HarassMenu["HarassQ"].Cast<CheckBox>().CurrentValue && Q.IsReady() && target.IsValidTarget(Q.Range))
            {
                Q.Cast();
            }
            if (E.IsReady() && target.IsValidTarget(E.Range) && _Player.ManaPercent > HarassMenu["manaHarass"].Cast<Slider>().CurrentValue)
                foreach (var eenemies in enemies)
                {
                    var useE = HarassMenu["HarassE"
                        + eenemies.ChampionName].Cast<CheckBox>().CurrentValue;
                    if (useE)
                    {
                        E.Cast(eenemies);
                    }
                }
        }
		
        private static void Combo()
        {
            var enemies = EntityManager.Heroes.Enemies.OrderByDescending
                (a => a.HealthPercent).Where(a => !a.IsMe && a.IsValidTarget() && a.Distance(_Player) <= E.Range);
            var target = TargetSelector.GetTarget(R.Range, DamageType.Physical);
            if (SpellMenu["ComboQ"].Cast<CheckBox>().CurrentValue && Q.IsReady() && target.IsValidTarget(Q.Range))
            {
                Q.Cast();
            }
            if (SpellMenu["ComboR"].Cast<CheckBox>().CurrentValue && R.IsReady() && target.IsValidTarget(R.Range) && target.Health + target.AttackShield < Player.Instance.GetSpellDamage(target, SpellSlot.R))
            {
                R.Cast(target);
            }
            if (E.IsReady() && target.IsValidTarget(E.Range))
            foreach (var eenemies in enemies)
            {
                var useE = SpellMenu["useECombo"
                    + eenemies.ChampionName].Cast<CheckBox>().CurrentValue;
                if (useE)
                {
                    E.Cast(eenemies);
                }
            }
        }


        private static void KillSteal()
        {
            var target = EntityManager.Heroes.Enemies.Where(e => e.IsValidTarget(1200) && !e.IsDead && !e.IsZombie && e.HealthPercent <= 25);
            foreach (var target2 in target)
	    	{
                if (StealMenu["RKs"].Cast<CheckBox>().CurrentValue && R.IsReady() && target2.Health + target2.AttackShield < Player.Instance.GetSpellDamage(target2, SpellSlot.R))
                {
                    R.Cast(target2);
                }
                if (SpellMenu["ComboER"].Cast<CheckBox>().CurrentValue && R.IsReady() && target2.Health + target2.AttackShield < Player.Instance.GetSpellDamage(target2, SpellSlot.R) && !target2.HasBuff("tristanaecharge"))
                {
                    R.Cast(target2);
                }
            }
        }

        private static void LaneClear()
        {
            var minion = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Instance.Position, _Player.AttackRange).FirstOrDefault(a => !a.IsDead);
            var minionE = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Instance.Position, _Player.AttackRange).FirstOrDefault(a => a.HasBuff("tristanaecharge"));
            var tower = EntityManager.Turrets.Enemies.FirstOrDefault(a => !a.IsDead && a.Distance(_Player) < _Player.AttackRange);
            if (minion == null)
                if (tower == null)
                    return;
            
            if (LaneMenu["ClearTower"].Cast<CheckBox>().CurrentValue && E.IsReady() && tower.IsValidTarget(E.Range) && _Player.ManaPercent > LaneMenu["manaFarm"].Cast<Slider>().CurrentValue)
            {
                E.Cast(tower);
            }

            if (LaneMenu["ClearE"].Cast<CheckBox>().CurrentValue && E.IsReady() && minion.IsValidTarget(E.Range) && _Player.ManaPercent > LaneMenu["manaFarm"].Cast<Slider>().CurrentValue)
            {
                if (LaneMenu["ClearTower"].Cast<CheckBox>().CurrentValue && !tower.IsValidTarget(E.Range))
                    E.Cast(minion);
                else if(!LaneMenu["ClearTower"].Cast<CheckBox>().CurrentValue)
                    E.Cast(minion);
            }
            if (LaneMenu["ClearQ"].Cast<CheckBox>().CurrentValue && Q.IsReady())
            {
                Q.Cast();
            }
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            var renga = EntityManager.Heroes.Enemies.Find(r => r.ChampionName.Equals("Rengar"));
            var khazix = EntityManager.Heroes.Enemies.Find(r => r.ChampionName.Equals("Khazix"));


            if (renga != null)
            {
                if (sender.Name == ("Rengar_LeapSound.troy") && Misc["antiRengar"].Cast<CheckBox>().CurrentValue && sender.Position.Distance(_Player) < R.Range)
                    R.Cast(renga);
            }


            if (khazix != null)
            {
                if (sender.Name == ("Khazix_Base_E_Tar.troy") && Misc["antiKZ"].Cast<CheckBox>().CurrentValue && sender.Position.Distance(_Player) <= 400)
                    R.Cast(khazix);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Misc["drawAA"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { Color = Color.GreenYellow, BorderWidth = 1, Radius = E.Range }.Draw(_Player.Position);
            }
            if (Misc["drawW"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { Color = Color.GreenYellow, BorderWidth = 1, Radius = W.Range }.Draw(_Player.Position);
            }
        }
		
        private static void OnLoadingComplete(EventArgs args)
        {
            if (!_Player.ChampionName.Contains("Tristana")) return;
            Chat.Print("Bristana Loaded!", Color.GreenYellow);
            Chat.Print("Good Luck!", Color.GreenYellow);
            Bootstrap.Init(null);
            uint level = (uint)Player.Instance.Level;
            Q = new Spell.Active(SpellSlot.Q, 550);
            W = new Spell.Skillshot(SpellSlot.W, 900, SkillShotType.Circular, 450, int.MaxValue, 180);
            E = new Spell.Targeted(SpellSlot.E, 550);
            R = new Spell.Targeted(SpellSlot.R, 550);

            Menu = MainMenu.AddMenu("Bristana", "Bristana");
            Menu.AddGroupLabel("Bristana");
            Menu.AddLabel(" FEATURES ");
            Menu.AddLabel(" Please Select E Before Play ! ");
            Menu.AddLabel(" Combo Mode");
            Menu.AddLabel(" Harass Mode ");
            Menu.AddLabel(" Drawing Mode ");
            Menu.AddLabel(" KillSteal Mode ");
            Menu.AddLabel(" LaneClear Mode");
            Menu.AddLabel(" Anti Gapcloser");
            Menu.AddLabel(" Flee Mode ");
            Menu.AddLabel(" Skin Hack ");
            
            SpellMenu = Menu.AddSubMenu("Combo Settings", "Combo");
            SpellMenu.AddGroupLabel("Combo Settings");
            SpellMenu.Add("ComboQ", new CheckBox("Spell [Q]"));
            SpellMenu.Add("ComboR", new CheckBox("Spell [R]"));
            SpellMenu.Add("ComboER", new CheckBox("Spell [ER]"));
            SpellMenu.AddLabel("Spell [E] on");
            foreach (var enemies in EntityManager.Heroes.Enemies.Where(i => !i.IsMe))
            {
                SpellMenu.Add("useECombo" + enemies.ChampionName, new CheckBox("" + enemies.ChampionName));
            }

            HarassMenu = Menu.AddSubMenu("Harass Settings", "Harass");
            HarassMenu.AddGroupLabel("Harass Settings");
            HarassMenu.Add("HarassQ", new CheckBox("Spell [Q]", false));
            HarassMenu.AddLabel("Spell [E] on");
            foreach (var enemies in EntityManager.Heroes.Enemies.Where(i => !i.IsMe))
            {
                HarassMenu.Add("HarassE" + enemies.ChampionName, new CheckBox("" + enemies.ChampionName));
            }
            HarassMenu.Add("manaHarass", new Slider("Min Mana For Harass", 50, 0, 100));

            LaneMenu = Menu.AddSubMenu("Laneclear Settings", "Clear");
            LaneMenu.AddGroupLabel("Laneclear Settings");
            LaneMenu.Add("ClearQ", new CheckBox("Spell [Q]", false));
            LaneMenu.Add("ClearE", new CheckBox("Spell [E]", false));
            LaneMenu.Add("ClearTower", new CheckBox("Spell [E] Turret", false));
            LaneMenu.Add("manaFarm", new Slider("Min Mana For LaneClear", 50, 0, 100));
			
            JungleMenu = Menu.AddSubMenu("JungleClear Settings", "JungleClear");
            JungleMenu.AddGroupLabel("JungleClear Settings");
            JungleMenu.Add("jungleQ", new CheckBox("Spell [Q]"));
            JungleMenu.Add("jungleE", new CheckBox("Spell [E]"));
            JungleMenu.Add("jungleW", new CheckBox("Spell [W]", false));
            JungleMenu.Add("manaJung", new Slider("Min Mana For JungleClear", 50, 0, 100));

            StealMenu = Menu.AddSubMenu("KillSteal Settings", "KS");
            StealMenu.AddGroupLabel("Killsteal Settings");
            StealMenu.Add("RKs", new CheckBox("Spell [R]"));



            Misc = Menu.AddSubMenu("Misc Settings", "Draw");
            Misc.AddGroupLabel("Anti Gapcloser");
            Misc.Add("antiGap", new CheckBox("Anti Gapcloser"));
            Misc.Add("antiRengar", new CheckBox("Anti Rengar"));
            Misc.Add("antiKZ", new CheckBox("Anti Kha'Zix"));
            Misc.AddGroupLabel("Drawings Settings");
            Misc.Add("drawAA", new CheckBox("Draw E"));
            Misc.Add("drawW", new CheckBox("Draw W", false));
			
            Skin = Menu.AddSubMenu("Skin Changer", "SkinChanger");
            Skin.Add("checkSkin", new CheckBox("Use Skin Changer"));
            Skin.Add("skin.Id", new ComboBox("Skin Mode", 0, "Classic", "Riot Tristana", "Earnest Elf Tristana", "Firefighter Tristana", "Guerilla Tristana", "Rocket Tristana", "Color Tristana", "Color Tristana", "Color Tristana", "Color Tristana", "Dragon Trainer Tristana"));


            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
            Gapcloser.OnGapcloser += Gapcloser_OnGapCloser;
            GameObject.OnCreate += GameObject_OnCreate;

		}
    }
}