using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using AppComponents.Extensions.EnumEx;
using AppComponents.Extensions.EnumerableEx;

#if UNITY_4_1
using JsonFx.Json;
using AppComponents.Data;
using UnityEngine;
#else
using PropertyTools;
using TAC.Wpf;

#endif

namespace LoK.ManagedApp
{
    using System.Xml.Serialization;

    using Raven.Imports.Newtonsoft.Json;

    public enum ScreenForms
    {
        Form16_9 = 1920,
        Form9_16 = 1080,
        Form4_3 = 1600,
        Form3_4 = 1200,
        Form1_1 = 1000,

    }

    public enum FormCharacter
    {
        VeryVertical = 515,
        Vertical = 666,
        Square = 1500,
        Horizontal = 2444,
        VeryHorizontal = 100000
    }

    public enum StandardViewPorts
    {
        Full,

        LeftThird,
        RightThird,
        MiddleThirdHorizontal,
        MiddleThirdVertical,
        BottomThird,
        TopThird,

        LeftTwoThirds,
        RightTwoThirds,
        TopTwoThirds,
        BottomTwoThirds,

        LeftHalf,
        RightHalf,
        TopHalf,
        BottomHalf,

        LeftFourth,
        RightFourth,
        TopFourth,
        BottomFourth,

        MiddleHalfHorizontal,
        MiddleHalfVertical,

        PantLegTL2R,
        PantLegTR2L,
        PantLegBL2R,
        PantLegBR2L,
        PantLegL2Dn,
        PantLegR2Dn,
        PantLegL2Up,
        PantLegR2Up



    }

    public enum StandardPlacementLayouts
    {
        Full,

        HorizontalThirds,
        HorizontalHalves,
        HorizontalSideBurns,
        Horizontal1_2,
        Horizontal2_1,

        VerticalThirds,
        VerticalHalves,
        VerticalSandwich,
        Vertical1_2,
        Vertical2_1,

        PantsLeft,
        PantsRight,
        PantsStand,
        PantsHandstand


    }

    public static class StandardPlacementLayoutSpecification
    {
        public static IEnumerable<StandardViewPorts> DescribeLayout(StandardPlacementLayouts layout)
        {
            switch (layout)
            {
                case StandardPlacementLayouts.Full:
                    return EnumerableEx.OfOne(StandardViewPorts.Full);

                case StandardPlacementLayouts.HorizontalThirds:
                    return EnumerableEx.OfThree(StandardViewPorts.LeftThird, StandardViewPorts.MiddleThirdVertical, StandardViewPorts.RightThird);

                case StandardPlacementLayouts.HorizontalHalves:
                    return EnumerableEx.OfTwo(StandardViewPorts.LeftHalf, StandardViewPorts.RightHalf);

                case StandardPlacementLayouts.HorizontalSideBurns:
                    return EnumerableEx.OfThree(StandardViewPorts.LeftFourth, StandardViewPorts.MiddleHalfVertical, StandardViewPorts.RightFourth);

                case StandardPlacementLayouts.Horizontal1_2:
                    return EnumerableEx.OfTwo(StandardViewPorts.LeftThird, StandardViewPorts.RightTwoThirds);

                case StandardPlacementLayouts.Horizontal2_1:
                    return EnumerableEx.OfTwo(StandardViewPorts.LeftTwoThirds, StandardViewPorts.RightThird);

                case StandardPlacementLayouts.VerticalThirds:
                    return EnumerableEx.OfThree(StandardViewPorts.TopThird, StandardViewPorts.MiddleThirdHorizontal, StandardViewPorts.BottomThird);

                case StandardPlacementLayouts.VerticalHalves:
                    return EnumerableEx.OfTwo(StandardViewPorts.TopHalf, StandardViewPorts.BottomHalf);

                case StandardPlacementLayouts.VerticalSandwich:
                    return EnumerableEx.OfThree(StandardViewPorts.TopFourth, StandardViewPorts.MiddleHalfHorizontal, StandardViewPorts.BottomFourth);

                case StandardPlacementLayouts.Vertical1_2:
                    return EnumerableEx.OfTwo(StandardViewPorts.TopThird, StandardViewPorts.BottomTwoThirds);

                case StandardPlacementLayouts.Vertical2_1:
                    return EnumerableEx.OfTwo(StandardViewPorts.TopTwoThirds, StandardViewPorts.BottomThird);

                case StandardPlacementLayouts.PantsLeft:
                    return EnumerableEx.OfThree(StandardViewPorts.LeftThird, StandardViewPorts.PantLegTR2L, StandardViewPorts.PantLegBR2L);

                case StandardPlacementLayouts.PantsRight:
                    return EnumerableEx.OfThree(StandardViewPorts.RightThird, StandardViewPorts.PantLegTL2R, StandardViewPorts.PantLegBL2R);

                case StandardPlacementLayouts.PantsStand:
                    return EnumerableEx.OfThree(StandardViewPorts.TopThird, StandardViewPorts.PantLegL2Up, StandardViewPorts.PantLegR2Up);

                case StandardPlacementLayouts.PantsHandstand:
                    return EnumerableEx.OfThree(StandardViewPorts.BottomThird, StandardViewPorts.PantLegL2Dn, StandardViewPorts.PantLegR2Dn);
            }

            return Enumerable.Empty<StandardViewPorts>();
        }

        public static PortalViewPort StandardViewPortForCharacter(FormCharacter fc, int fixedWidth)
        {
            int height = (1000 * fixedWidth) / fc.EnumValue();
            return new PortalViewPort { Left = 0, Top = 0, Width = fixedWidth, Height = height };
        }

        public static PortalViewPort StandardViewPortForCharacter(FormCharacter fc, ScreenForms sf)
        {
            return StandardViewPortForCharacter(fc, sf.EnumValue());
        }

        public static PortalViewPort ScreenForm(ScreenForms sf)
        {
            int height = 0;
            switch (sf)
            {
                case ScreenForms.Form9_16:
                    height = ScreenForms.Form16_9.EnumValue();
                    break;
                case ScreenForms.Form16_9:
                    height = ScreenForms.Form9_16.EnumValue();
                    break;
                case ScreenForms.Form4_3:
                    height = ScreenForms.Form3_4.EnumValue();
                    break;
                case ScreenForms.Form3_4:
                    height = ScreenForms.Form4_3.EnumValue();
                    break;
            }
            return new PortalViewPort { Left = 0, Top = 0, Width = sf.EnumValue(), Height = height };
        }
    }


    /// <summary>
    /// Describes the shape of a sign in terms of its width/height
    /// aspect ratio. 
    /// </summary>
    public class PortalForm : Observable
    {
        public PortalForm()
        {

        }

        public PortalForm(ScreenForms scrForm)
        {
            int w = 1000, h = 1000;
            switch (scrForm)
            {
                case ScreenForms.Form16_9:
                    w = 16;
                    h = 9;
                    break;

                case ScreenForms.Form9_16:
                    w = 9;
                    h = 16;
                    break;

                case ScreenForms.Form4_3:
                    w = 4;
                    h = 3;
                    break;

                case ScreenForms.Form3_4:
                    w = 3;
                    h = 4;
                    break;
            }

            Ratio = (1000 * w) / h;
        }

        public PortalForm(int widthR, int heightR)
        {
            SetRatio(widthR, heightR);
        }

        public void SetRatio(int width, int height)
        {
            if (height != 0)
                Ratio = (1000 * width) / height;
        }


        private int _ratio;
#if !UNITY_4_1

        [ReadOnly(true)]
        [DisplayName("Ratio")]
        [Description("Ratio of width to height, scaled up by 1000")]
#endif
        public int Ratio
        {
            get { return _ratio; }
            private set { SetValue(ref _ratio, value, "Ratio"); RaisePropertyChanged("Character"); }
        }

        //ignore ravendb serialization
        [JsonIgnore]
        //ignore newtownsoft json serialization
        [Newtonsoft.Json.JsonIgnore]
        //ignore xml serialization
        [XmlIgnore]

#if !UNITY_4_1
        [DisplayName("Shape Character")]
        [ReadOnly(true)]
        [Description("Characterization of this form")]
#endif
        public FormCharacter Character
        {
            get
            {
                foreach (FormCharacter it in Enum.GetValues(typeof(FormCharacter)))
                {
                    if (Ratio <= it.EnumValue())
                        return it;
                }

                return FormCharacter.VeryHorizontal;
            }
        }

        public bool CloseInCharacter(FormCharacter other)
        {
            var numericCharacter = (int)other;
            return Math.Abs(numericCharacter - Ratio) < (Ratio / 4);
        }

        public override string ToString()
        {
            return Character.ToString();
        }

    }

    /// <summary>
    /// Given a window or viewport, represents a rectangle viewport
    /// nested within that area
    /// </summary>
    public class PortalViewPort : Observable
    {
        public PortalViewPort()
        {
        }

        public PortalViewPort(FormCharacter fc, ScreenForms sf)
        {
            DesignSurface(fc, sf);
        }

        public PortalViewPort(ScreenForms sf)
        {
            DesignSurface(sf);
        }

        public PortalViewPort(FormCharacter fc)
        {
            DesignSurface(fc);
        }


        private int _left;
        public int Left
        {
            get { return _left; }
            set { SetValue(ref _left, value, "Left"); RaisePropertyChanged("Right"); }
        }

        private int _top;
        public int Top
        {
            get { return _top; }
            set { SetValue(ref _top, value, "Top"); RaisePropertyChanged("Bottom"); }
        }

        private int _width;
        public int Width
        {
            get { return _width; }
            set { SetValue(ref _width, value, "Width"); _form.SetRatio(_width, _height); }
        }

        private int _height;
        public int Height
        {
            get { return _height; }
            set { SetValue(ref _height, value, "Height"); _form.SetRatio(_width, _height); }
        }



        PortalForm _form = new PortalForm();

        //ignore ravendb serialization
        [JsonIgnore]
        //ignore newtownsoft json serialization
        [Newtonsoft.Json.JsonIgnore]
        //ignore xml serialization
        [XmlIgnore]
#if !UNITY_4_1
        [NestedProperties]
        [ReadOnly(true)]
#endif
        public PortalForm Form
        {
            get { return _form; }
            set { SetValue(ref _form, value, "Form"); }
        }

        //ignore ravendb serialization
        [JsonIgnore]
        //ignore newtownsoft json serialization
        [Newtonsoft.Json.JsonIgnore]
        //ignore xml serialization
        [XmlIgnore]
        public int Right
        {
            get
            {
                return Left + Width;
            }
        }

        //ignore ravendb serialization
        [JsonIgnore]
        //ignore newtownsoft json serialization
        [Newtonsoft.Json.JsonIgnore]
        //ignore xml serialization
        [XmlIgnore]
        public int Bottom
        {
            get
            {
                return Top + Height;
            }
        }

        public PortalViewPort InScreenSpace(int parentHeight)
        {
            // in screen space, origin is bottom left, and the y axis is positive going up.

            var t = parentHeight - _top;


            return new PortalViewPort
            {
                Top = t,
                Left = _left,
                Height = _height,
                Width = _width
            };
        }

        public void AsPercentageOf(int width, int height, out float l, out float t, out float w, out float h)
        {
#if UNITY_4_1

			l = Mathf.Clamp( ( (float) _left) / ( (float) width ), 0f, 1f);
			t = Mathf.Clamp( ( (float) _top) / ((float) height), 0f, 1f);
			
			w = Mathf.Clamp( ( (float) _width) / ((float) width), 0f, 1f);
			if(w > 0.98f) w = 1f;
			
			h = Mathf.Clamp( ( (float) _height) / ((float) height), 0f, 1f);
			if(h > 0.98f) h = 1f;
#else
            l = 0f;
            t = 0f;
            w = 0f;
            h = 0f;
#endif

        }

        public void DesignSurface(FormCharacter fc, ScreenForms sf)
        {
            var nvp = StandardPlacementLayoutSpecification.StandardViewPortForCharacter(fc, sf);
            Width = nvp.Width;
            Height = nvp.Height;
        }

        public void DesignSurface(ScreenForms sf)
        {
            var nvp = StandardPlacementLayoutSpecification.ScreenForm(sf);
            Width = nvp.Width;
            Height = nvp.Height;
        }

        public void DesignSurface(FormCharacter fc)
        {
            var nvp = StandardPlacementLayoutSpecification.StandardViewPortForCharacter(fc, ScreenForms.Form1_1);
            Width = nvp.Width;
            Height = nvp.Height;
        }

        public void GetRescaleRatios(int givenWidth, int givenHeight, out float widthRatio, out float heightRatio)
        {
            widthRatio = ((float)givenWidth) / ((float)_width);
            heightRatio = ((float)givenHeight) / ((float)_height);

        }

        public void RescalePortal(float wr, float hr)
        {
            _left = (int)Math.Floor(_left * wr);
            _top = (int)Math.Floor(_top * hr);
            _width = (int)Math.Floor(_width * wr);
            _height = (int)Math.Floor(_height * hr);
        }

        public void RescalePortal(int givenWidth, int givenHeight)
        {
            float wr, hr;
            GetRescaleRatios(givenWidth, givenHeight, out wr, out hr);
            RescalePortal(wr, hr);

        }

        public PortalViewPort NestViewPort(StandardViewPorts vps)
        {
            int l = 0, t = 0, w = 0, h = 0;

            switch (vps)
            {
                case StandardViewPorts.Full:
                    l = Left;
                    t = Top;
                    w = Width;
                    h = Height;
                    break;

                case StandardViewPorts.LeftThird:
                    l = Left;
                    t = Top;
                    w = Width / 3;
                    h = Height;
                    break;

                case StandardViewPorts.RightThird:
                    l = (Left + 2 * (Width / 3));
                    t = Top;
                    w = Width / 3;
                    h = Height;
                    break;

                case StandardViewPorts.MiddleThirdHorizontal:
                    l = Left;
                    t = (Top + (Height / 3));
                    w = Width;
                    h = Height / 3;
                    break;

                case StandardViewPorts.MiddleThirdVertical:
                    l = (Left + (Width / 3));
                    t = Top;
                    w = Width / 3;
                    h = Height;
                    break;

                case StandardViewPorts.BottomThird:
                    l = Left;
                    t = (Top + 2 * (Height / 3));
                    w = Width;
                    h = Height / 3;
                    break;

                case StandardViewPorts.TopThird:
                    l = Left;
                    t = Top;
                    w = Width;
                    h = Height / 3;
                    break;

                case StandardViewPorts.LeftTwoThirds:
                    l = Left;
                    t = Top;
                    w = Width - (Width / 3);
                    h = Height;
                    break;

                case StandardViewPorts.RightTwoThirds:
                    l = Left + (Width / 3);
                    t = Top;
                    w = Width - (Width / 3);
                    h = Height;
                    break;

                case StandardViewPorts.TopTwoThirds:
                    l = Left;
                    t = Top;
                    h = Height - (Height / 3);
                    w = Width;
                    break;

                case StandardViewPorts.BottomTwoThirds:
                    l = Left;
                    t = Top + (Height / 3);
                    h = Height - (Height / 3);
                    w = Width;
                    break;

                case StandardViewPorts.LeftHalf:
                    l = Left;
                    t = Top;
                    w = Width / 2;
                    h = Height;
                    break;

                case StandardViewPorts.RightHalf:
                    l = Left + (Width / 2);
                    t = Top;
                    w = Width / 2;
                    h = Height;
                    break;

                case StandardViewPorts.TopHalf:
                    l = Left;
                    t = Top;
                    h = Height / 2;
                    w = Width;
                    break;

                case StandardViewPorts.BottomHalf:
                    l = Left;
                    t = Top + (Height / 2);
                    h = Height / 2;
                    w = Width;
                    break;

                case StandardViewPorts.LeftFourth:
                    l = Left;
                    t = Top;
                    w = Width / 4;
                    h = Height;
                    break;

                case StandardViewPorts.RightFourth:
                    l = Left + (3 * (Width / 4));
                    t = Top;
                    w = Width / 4;
                    h = Height;

                    break;

                case StandardViewPorts.TopFourth:
                    l = Left;
                    t = Top;
                    h = Height / 4;
                    w = Width;
                    break;

                case StandardViewPorts.BottomFourth:
                    l = Left;
                    t = Top + (3 * (Height / 4));
                    h = Height / 4;
                    w = Width;
                    break;

                case StandardViewPorts.MiddleHalfHorizontal:
                    l = Left;
                    t = Top + (Height / 4);
                    h = Height / 2;
                    w = Width;
                    break;

                case StandardViewPorts.MiddleHalfVertical:
                    l = Left + (Width / 4);
                    t = Top;
                    h = Height;
                    w = Width / 2;
                    break;

                case StandardViewPorts.PantLegTL2R:
                    l = Left;
                    t = Top;
                    w = 2 * (Width / 3);
                    h = Height / 2;
                    break;

                case StandardViewPorts.PantLegTR2L:
                    l = Left + (Width / 3);
                    t = Top;
                    w = 2 * (Width / 3);
                    h = Height / 2;
                    break;

                case StandardViewPorts.PantLegBL2R:
                    l = Left;
                    t = Top + (Height / 2);
                    w = 2 * (Width / 3);
                    h = Height / 2;
                    break;

                case StandardViewPorts.PantLegBR2L:
                    l = Left + (Width / 3);
                    t = Top + (Height / 2);
                    w = 2 * (Width / 3);
                    h = Height / 2;
                    break;

                case StandardViewPorts.PantLegL2Dn:
                    l = Left;
                    t = Top;
                    w = Width / 2;
                    h = 2 * (Height / 3);
                    break;

                case StandardViewPorts.PantLegR2Dn:
                    l = Left + (Width / 2);
                    t = Top;
                    w = Width / 2;
                    h = 2 * (Height / 3);
                    break;

                case StandardViewPorts.PantLegL2Up:
                    l = Left;
                    t = Top + (Height / 3);
                    w = Width / 2;
                    h = 2 * (Height / 3);
                    break;

                case StandardViewPorts.PantLegR2Up:
                    l = Left + (Width / 2);
                    t = Top + (Height / 3);
                    w = Width / 2;
                    h = 2 * (Height / 3);
                    break;
            }

            return new PortalViewPort { Left = l, Top = t, Width = w, Height = h };
        }

        public IEnumerable<PortalViewPort> DivideByLayout(StandardPlacementLayouts layout)
        {
            var lpvp = new List<PortalViewPort>();
            foreach (var svp in StandardPlacementLayoutSpecification.DescribeLayout(layout))
            {
                lpvp.Add(this.NestViewPort(svp));
            }

            return lpvp;
        }

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            PortalViewPort p = obj as PortalViewPort;
            if ((System.Object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return Top == p.Top && Left == p.Left && Height == p.Height && Width == p.Width;
        }

        public bool Equals(PortalViewPort p)
        {
            // If parameter is null return false:
            if (p == null)
            {
                return false;
            }

            // Return true if the fields match:
            // Return true if the fields match:
            return Top == p.Top && Left == p.Left && Height == p.Height && Width == p.Width;
        }

        public override int GetHashCode()
        {
            return Top ^ Left ^ Height ^ Bottom;
        }

        public override string ToString()
        {
            return string.Format("Portal [Left: {0}, Top: {1}, Width: {2}, Height: {3}]", Left, Top, Width, Height);
        }

    }

    /// <summary>
    /// Describes a division of the application window which provides 
    /// space for signs.
    /// </summary>
    public class PortalPlacement : Observable
    {
        public PortalPlacement()
        {
            Identifier = Guid.NewGuid().ToString();
            Viewport = new PortalViewPort();
        }

        private string _identifier;
#if !UNITY_4_1


        [Category("Summary")]
        [ReadOnly(true)]
#endif
        public string Identifier
        {
            get { return _identifier; }
            set { SetValue(ref _identifier, value, "Identifier"); }
        }

        private bool _isPremium;

        /// <summary>
        /// Premium placement ads can go here
        /// </summary>	

#if !UNITY_4_1
        [Category("Behavior")]
#endif
        public bool IsPremium
        {
            get { return _isPremium; }
            set { SetValue(ref _isPremium, value, "IsPremium"); }
        }


        private bool _controlsAudio;

        /// <summary>
        /// Audio for signs in this placement 
        /// during the lead sign play, 
        /// is played instead of muted.
        /// </summary>

#if !UNITY_4_1
        [Category("Behavior")]
#endif
        public bool ControlsAudio
        {
            get { return _controlsAudio; }
            set { SetValue(ref _controlsAudio, value, "ControlsAudio"); }
        }

        private PortalViewPort _viewport;

        /// <summary>
        /// Area of the sign window that defines this placement.
        /// </summary>

#if !UNITY_4_1
        [Category("Presentation")]
        [NestedProperties]
#endif
        public PortalViewPort Viewport
        {
            get { return _viewport; }
            set { SetValue(ref _viewport, value, "Viewport"); }
        }

#if !UNITY_4_1
        [Category("Presentation")]
        [NestedProperties]
#endif
        private StandardViewPorts _standardViewPort;
        public StandardViewPorts StandardViewPort
        {
            get { return _standardViewPort; }
            set { SetValue(ref _standardViewPort, value, "StandardViewPort"); }
        }

        private int _padding;

#if !UNITY_4_1
        [Category("Presentation")]

#endif
        public int Padding
        {
            get { return _padding; }
            set { SetValue(ref _padding, value, "Padding"); }
        }
    }
}