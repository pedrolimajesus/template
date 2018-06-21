using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents;
using AppComponents.Dynamic.Lambdas;
using AppComponents.Dynamic.Projection;

namespace TACPlaygroundClasses
{

    public enum MemberNames
    {
        MyShip,
        MyCaptain
    };

    public interface IXFactor
    {
        bool XFactorIt();
    }

    public enum Sides
    {
        Forward,
        Aft,
        Port,
        Starboard,
        Above,
        Below
    };

    public class Laser
    {
        public int PowerLevel { get; set; }
        public bool Operational { get; set; }
        public Sides Facing { get; set; }
        public IEnumerable<Sides> ValidArc { get; set; }
    }

    public class Ship : IShip
    {
        private readonly Dictionary<Sides, int> _armorLevel = new Dictionary<Sides, int>();
        private readonly Dictionary<Sides, int> _hullIntegrity = new Dictionary<Sides, int>();
        private readonly List<Laser> _lasers = new List<Laser>();
        private readonly Dictionary<Sides, int> _shieldsLevel = new Dictionary<Sides, int>();

        #region IShip Members

        public string Name { get; set; }
        public string Registry { get; set; }

        public Tuple<float, float, float> LocalSectorPosition { get; set; }
        public Tuple<float, float, float> Sector { get; set; }
        public Tuple<float, float, float> Heading { get; set; }
        public Tuple<float, float, float> Velocity { get; set; }

        public int Fuel { get; set; }

        public Captain Captain { get; set; }

        public Dictionary<Sides, int> Shields
        {
            get { return _shieldsLevel; }
        }

        public Dictionary<Sides, int> Armor
        {
            get { return _armorLevel; }
        }

        public Dictionary<Sides, int> HullIntegrity
        {
            get { return _hullIntegrity; }
        }

        public int Missiles { get; set; }

        public IEnumerable<Laser> Lasers
        {
            get { return _lasers; }
        }


        public void FullStop()
        {
        }

        public void LockOnTarget(Ship otherShip)
        {
        }

        public bool CriticalDamage()
        {
            return default(bool);
        }

        public bool Attack(bool useMissiles, bool useLasers, bool defensivePosture)
        {
            return default(bool);
        }

        public string RecieveMessage(string otherShipRegistry, bool translation)
        {
            return default(string);
        }

        #endregion
    }

    public class Captain
    {
        public string Name { get; set; }
        public int AggressionLevel { get; set; }
        public int DeceptionLevel { get; set; }
        public int Braveness { get; set; }

        public void Insult(Captain other)
        {
        }

        public void AbandonShip()
        {
        }

        public string Preach()
        {
            return default(string);
        }

        public int CrewWantToMutinyCount(int arrogance, bool oblivious, int dangerLevel, string rallySpeech)
        {
            return default(int);
        }
    }

    public class SensorElement
    {
        #region Properties

        public string Category { get; set; }

        public string Display { get; set; }

        public Tuple<float, float, float> Position { get; set; }

        public bool Visible { get; set; }

        public int Color { get; set; }

        public int Clarity { get; set; }

        #endregion

        public override string ToString()
        {
            string str = String.Empty;
            str = String.Concat(str, "Category = ", Category, "\r\n");
            str = String.Concat(str, "Display = ", Display, "\r\n");
            str = String.Concat(str, "Position = ", Position, "\r\n");
            str = String.Concat(str, "Visible = ", Visible, "\r\n");
            str = String.Concat(str, "Color = ", Color, "\r\n");
            str = String.Concat(str, "Clarity = ", Clarity, "\r\n");
            return str;
        }
    }

    public interface IShip
    {
        Dictionary<Sides, int> Armor { get; }
        Captain Captain { get; set; }
        int Fuel { get; set; }
        Tuple<float, float, float> Heading { get; set; }
        Dictionary<Sides, int> HullIntegrity { get; }
        IEnumerable<Laser> Lasers { get; }
        Tuple<float, float, float> LocalSectorPosition { get; set; }
        int Missiles { get; set; }
        string Name { get; set; }
        string Registry { get; set; }
        Tuple<float, float, float> Sector { get; set; }
        Dictionary<Sides, int> Shields { get; }
        Tuple<float, float, float> Velocity { get; set; }
        bool Attack(bool useMissiles, bool useLasers, bool defensivePosture);
        bool CriticalDamage();
        void FullStop();
        void LockOnTarget(Ship otherShip);
        string RecieveMessage(string otherShipRegistry, bool translation);
    }

    public class TypeProjectionPlayground
    {
        private void Stuff()
        {



            var pfact = TypeProjector.Create()
                .SelectFrom<IShip>(t => t.Property(s => s.Fuel, 0.0F),
                                   t => t.Method(s => s.FullStop(), Return<int>.ThisAndArguments(@this => 0)))
                .Construct(builder => builder.Prototype(
                    Nonsense: "Lugrgar",
                    Transform: Return<string>.ThisAndArguments<string>((@this, m) => @this.Xform(m)),
                    DoStuff: Return<int>.ThisAndArguments<string, Ship>((@this, name, s) => { return 0; }),
                    DamageIt: ReturnVoid.ThisAndArguments<Ship, Laser, Captain>(
                        (@this, aShip, laser, captain) =>
                        {
                            aShip.Captain = captain;
                            aShip.HullIntegrity[Sides.Above] -= laser.PowerLevel;
                            @this.SplatOnScreen(aShip, laser);
                        }),
                    XFactorIt: Return<bool>.ThisAndArguments(@this => false)))
                .EmbedInstanceOf<IShip>(MemberNames.MyShip)
                .EmbedAndExpose<Captain>(MemberNames.MyCaptain,
                                         t => t.Properties(
                                             c => c.AggressionLevel,
                                             c => c.Braveness,
                                             c => c.DeceptionLevel),
                                         t => t.Methods(
                                             c => c.AbandonShip()
                                                  ))
                .Initializer(
                    ReturnVoid.ThisAndArguments(
                        @this =>
                        {
                            @this.InitialData = Tuple.Create(0, false, MemberNames.MyCaptain, string.Empty,
                                                             new Laser());
                        }))
                .Declare()
                .CreateFactoryDressedAs<IXFactor>();

            Catalog.Services.Register(_ => pfact());



            var ixf = Catalog.Factory.Resolve<IXFactor>();
            bool good = ixf.XFactorIt();
        }
    }
}
