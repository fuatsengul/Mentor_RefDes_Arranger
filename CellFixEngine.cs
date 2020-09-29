using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MGCPCB;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xPCB_RefDesArranger
{
    public class CellFixEngine
    {

        public const int TextLineWidth = 1;
        public const int AssemblyLineWidth = 2;
        public const int SilkScreenLineWidth = 5;
        public const int SilkScreenRefDesHeight = 30;

        public void BatchFixComponent( ref MGCPCB.Component _cellDoc )
        {
            List<Point> Points = new List<Point>();
            #region Point Motor Function

            //foreach ( FabricationLayerGfx gfx in _cellDoc.get_FabricationLayerGfxs(EPcbFabricationType.epcbFabSilkscreen) )
            //{
            //    if ( !gfx.Geometry.Filled )
            //    {
            //        gfx.Geometry.set_LineDisplayWidth(EPcbUnit.epcbUnitMils, SilkScreenLineWidth);
            //        gfx.Geometry.set_LineWidth(EPcbUnit.epcbUnitMils, SilkScreenLineWidth);
            //    }
            //}

            //foreach ( FabricationLayerText _txt in _cellDoc.get_FabricationLayerTexts(EPcbFabricationType.epcbFabSilkscreen) )
            //{
            //    //Console.WriteLine(_txt.TextString);
            //    if ( _txt.TextString == _cellDoc.RefDes )
            //    {
            //        //_txt.Format.set_Height(EPcbUnit.epcbUnitMils, SilkScreenRefDesHeight);
            //        //_txt.Format.set_PenWidth(EPcbUnit.epcbUnitMils, SilkScreenLineWidth);
            //        _txt.Format.Font = "VeriBest Gerber 0";
            //        _txt.Format.set_PenWidth(EPcbUnit.epcbUnitMils, SilkScreenLineWidth);
            //        _txt.Format.AspectRatio = 1;
            //    }
            //}

            double orientation = _cellDoc.get_Orientation(EPcbAngleUnit.epcbAngleUnitDegrees);
            //bool orientationChanged = false;

            _cellDoc.PlacementOutlines[1].Geometry.set_LineDisplayWidth(EPcbUnit.epcbUnitMils, 0);
            

            if ( orientation % 90 != 0 )
            {
                return;
            }

            foreach ( FabricationLayerGfx gfx in _cellDoc.get_FabricationLayerGfxs(EPcbFabricationType.epcbFabAssembly) )
            {
                gfx.Geometry.set_LineDisplayWidth(EPcbUnit.epcbUnitMils, AssemblyLineWidth);
                Points = ProcessGFX(gfx.Geometry, Points);
            }

            if ( _cellDoc.get_FabricationLayerGfxs(EPcbFabricationType.epcbFabAssembly).Count < 1 )
            {
                Points = ProcessGFX(_cellDoc.PlacementOutlines[1].Geometry, Points);
            }

            #endregion
            if ( Points.Count > 1000 )
            {
                Console.WriteLine("Not Processed: " + _cellDoc.Name);
                return;
            }

            #region rectangle find motor
            List<Rectangle> Rectangles = new List<Rectangle>();
            foreach ( Point _point in Points )
            {
                if ( _point.X != 0 || _point.Y != 0 )
                    foreach ( Point _point2 in Points )
                    {
                        if ( _point2.X != 0 && _point2.Y != 0 )
                            if ( _point.X != _point2.X && _point.Y != _point2.Y )
                            {

                                int minX = 0, maxX = 0;
                                int minY = 0, maxY = 0;

                                if ( _point.X < _point2.X )
                                {
                                    minX = _point.X;
                                    maxX = _point2.X;
                                }
                                else
                                {
                                    minX = _point2.X;
                                    maxX = _point.X;
                                }
                                if ( _point.Y < _point2.Y )
                                {
                                    minY = _point.Y;
                                    maxY = _point2.Y;
                                }
                                else
                                {
                                    minY = _point2.Y;
                                    maxY = _point.Y;
                                }

                                bool IsAcceptable = true;
                                #region Validate rectangle is free
                                foreach ( Point _point3 in Points )
                                {
                                    if ( minX < _point3.X && _point3.X < maxX )
                                    {
                                        if ( minY < _point3.Y && _point3.Y < maxY )
                                        {
                                            IsAcceptable = false;
                                            break;
                                        }
                                    }
                                }
                                #endregion
                                if ( IsAcceptable )
                                    Rectangles.Add(new Rectangle(minX, minY, maxX - minX, maxY - minY));

                            }
                    }
            }

            Rectangle theBestRectangle = new Rectangle();
            foreach ( Rectangle _rect in Rectangles )
            {
                int area1 = _rect.Width * _rect.Height;
                bool greatisThis = true;
                foreach ( Rectangle _rect2 in Rectangles )
                {
                    int area2 = _rect2.Width * _rect2.Height;
                    if ( area2 > area1 )
                    {
                        greatisThis = false;
                        break;
                    }

                }
                if ( greatisThis )
                {
                    theBestRectangle = _rect;
                    break;
                }

            }
            #endregion
            #region Text Repair Engine

            double gfx_X1 = 0, gfx_X2 = 0, gfx_Y1 = 0, gfx_Y2 = 0;
            double centerpointX = 0;
            double centerpointY = 0;
            double width = 0;
            double height = 0;
            double text_height = 0;

            gfx_X1 = theBestRectangle.X;
            gfx_X2 = theBestRectangle.X + theBestRectangle.Width;

            gfx_Y1 = theBestRectangle.Y;
            gfx_Y2 = theBestRectangle.Y + theBestRectangle.Height;

            width = gfx_X2 - gfx_X1;
            height = gfx_Y2 - gfx_Y1;

            centerpointX = ( gfx_X1 + gfx_X2 ) / 2;
            centerpointY = ( gfx_Y1 + gfx_Y2 ) / 2;


            bool _90deg = false;
            if ( height > width )
            {
                _90deg = true;
                text_height = width - 10;
            }
            else
            {
                _90deg = false;
                text_height = height - 10;
            }

            if ( text_height > 100 )
                text_height = 100;

            if ( height > 5 )
                foreach ( FabricationLayerText text in _cellDoc.get_FabricationLayerTexts(EPcbFabricationType.epcbFabAssembly))
                {
                    text.Format.Font = "vf_std";
                    text.Format.set_PenWidth(EPcbUnit.epcbUnitMils, TextLineWidth);
                    text.Format.AspectRatio = 1;
                    if ( text.TextString == _cellDoc.RefDes )
                    {
                        
                        text.set_PositionX(EPcbUnit.epcbUnitMils, centerpointX);
                        text.set_PositionY(EPcbUnit.epcbUnitMils, centerpointY);
                        if ( _90deg )
                            text.Format.set_Orientation(EPcbAngleUnit.epcbAngleUnitDegrees, 90);
                        else
                            text.Format.set_Orientation(EPcbAngleUnit.epcbAngleUnitDegrees, 0);
                        text.Format.set_Height(EPcbUnit.epcbUnitMils, text_height);
                        text.Format.HorizontalJust = EPcbHorizontalJustification.epcbJustifyHCenter;
                        text.Format.VerticalJust = EPcbVerticalJustification.epcbJustifyVCenter;
                        try
                        {
                            if ( _90deg )
                            {
                                for ( ;; )
                                {
                                    Size s = TextRenderer.MeasureText(text.TextString, new Font("vf_std", (float)text_height));
                                    if ( s.Width > height -5 && text_height > 10 )
                                    {
                                        text_height -= 5;
                                    }
                                    else
                                    {
                                        text.Format.set_Height(EPcbUnit.epcbUnitMils, text_height);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                for ( ;; )
                                {
                                    Size s = TextRenderer.MeasureText(text.TextString, new Font("vf_std", (float)text_height));
                                    if ( s.Width > width -5 && text_height > 10 )
                                    {
                                        text_height -= 5;
                                    }
                                    else
                                    {
                                        text.Format.set_Height(EPcbUnit.epcbUnitMils, text_height);
                                        break;
                                    }
                                }
                            }
                        }
                        catch ( Exception m )
                        {
                            System.Windows.Forms.MessageBox.Show(m.Message);
                            throw m;
                        }

                    }
                    else
                    {
                        if ( text.Format.get_Height(EPcbUnit.epcbUnitMils) > 25 )
                            text.Format.set_Height(EPcbUnit.epcbUnitMils, 25);
                    }
                }
            #endregion
            UserLayerTexts _texts = _cellDoc.get_UserLayerTexts(EPcbSelectionType.epcbSelectAll);
            foreach ( UserLayerText _text in _texts )
            {
                _text.Format.set_PenWidth(EPcbUnit.epcbUnitMils, 2);
            }

            //if ( orientationChanged)
            //{
            //    _cellDoc.set_Orientation(EPcbAngleUnit.epcbAngleUnitDegrees, orientation);
            //}
        }

        public void CopyUserLayerGFXtoAssembly( ref MGCPCB.Document _cellDoc, string _userLayerName )
        {
            foreach ( UserLayerGfx _gfx in _cellDoc.get_UserLayerGfxs() )
            {
                if ( _gfx.UserLayer.Name == _userLayerName )
                {
                    _gfx.Geometry.set_LineDisplayWidth(EPcbUnit.epcbUnitMils, 2);

                    object[,] pntArr = (object[,])_gfx.Geometry.get_PointsArray(EPcbUnit.epcbUnitMils);
                    var pnts = _gfx.Geometry.get_PointsArray(EPcbUnit.epcbUnitMils);
                    int len = pntArr.Length / 3;

                    _cellDoc.PutFabricationLayerGfx(
                        EPcbFabricationType.epcbFabAssembly,
                        EPcbSide.epcbSideMount,
                        2,
                        len,
                        ref pnts,
                        false,
                        null,
                        EPcbUnit.epcbUnitMils
                        );

                }
            }
        }

        public List<Point> ProcessGFX( Geometry _geom, List<Point> Points )
        {
            if ( _geom.IsRect() )
            {
                double stepX = ( _geom.get_RectMaxX(EPcbUnit.epcbUnitMils) - _geom.get_RectMinX(EPcbUnit.epcbUnitMils) ) / 25;
                for ( double x = _geom.get_RectMinX(EPcbUnit.epcbUnitMils); x <= _geom.get_RectMaxX(EPcbUnit.epcbUnitMils);x = x + stepX )
                {
                    Points.Add(new Point(Convert.ToInt16(x), Convert.ToInt16(_geom.get_RectMaxY(EPcbUnit.epcbUnitMils))));
                    Points.Add(new Point(Convert.ToInt16(x), Convert.ToInt16(_geom.get_RectMinY(EPcbUnit.epcbUnitMils))));
                }
                double stepY = ( _geom.get_RectMaxY(EPcbUnit.epcbUnitMils) - _geom.get_RectMinY(EPcbUnit.epcbUnitMils) ) / 25;
                for ( double y = _geom.get_RectMinY(EPcbUnit.epcbUnitMils);y <= _geom.get_RectMaxY(EPcbUnit.epcbUnitMils);y = y + stepY )
                {
                    Points.Add(new Point(Convert.ToInt16(_geom.get_RectMaxX(EPcbUnit.epcbUnitMils)), Convert.ToInt16(y)));
                    Points.Add(new Point(Convert.ToInt16(_geom.get_RectMinX(EPcbUnit.epcbUnitMils)), Convert.ToInt16(y)));
                }
            }
            else if ( _geom.IsCircle() )
            {
                double circx = _geom.get_CircleX(EPcbUnit.epcbUnitMils);
                double circy = _geom.get_CircleY(EPcbUnit.epcbUnitMils);
                double circR = _geom.get_CircleR(EPcbUnit.epcbUnitMils);
                for ( double pi = 0;pi < 2 * Math.PI;pi = pi + 0.1 )
                {
                    Points.Add(new Point(Convert.ToInt16(circx + ( circR * Math.Sin(pi) )), Convert.ToInt16(circy + ( circR * Math.Cos(pi) ))));
                }
            }
            else if ( _geom.IsArced() )
            {
                object[,] outlinepoints = (object[,])_geom.get_PointsArray(EPcbUnit.epcbUnitMils);
                string x = string.Empty;
                if ( outlinepoints.GetLength(1) == 3 )
                {
                    #region Arc points find
                    double centerX = Convert.ToDouble(outlinepoints[0, 1]);
                    double centerY = Convert.ToDouble(outlinepoints[1, 1]);

                    double pnt1X = Convert.ToDouble(outlinepoints[0, 0]);
                    double pnt2X = Convert.ToDouble(outlinepoints[0, 2]);
                    double pnt1Y = Convert.ToDouble(outlinepoints[1, 0]);
                    double pnt2Y = Convert.ToDouble(outlinepoints[1, 2]);

                    double dx1 = centerX - pnt1X;
                    double dx2 = centerX - pnt2X;

                    double dy1 = centerY - pnt1Y;
                    double dy2 = centerY - pnt2Y;

                    double dxdy1 = ( Math.Abs(dx1) * Math.Abs(dx1) ) + ( Math.Abs(dy1) * Math.Abs(dy1) );
                    double dxdy2 = ( Math.Abs(dx2) * Math.Abs(dx2) ) + ( Math.Abs(dy2) * Math.Abs(dy2) );

                    double dR1 = System.Math.Sqrt(dxdy1);
                    double dR2 = System.Math.Sqrt(dxdy2);

                    double alpha1 = 0;
                    double alpha2 = 0;
                    #endregion
                    #region alpha1 region find
                    if ( centerX < pnt1X && centerY < pnt1Y )
                        alpha1 = Math.Abs(Math.Atan(dy1 / dx1)) + Math.PI * 0; //1.bölge
                    else if ( centerX > pnt1X && centerY < pnt1Y )
                        alpha1 = Math.Abs(Math.Atan(dx1 / dy1)) + Math.PI * 0.5; //2.bölge
                    else if ( centerX > pnt1X && centerY > pnt1Y )
                        alpha1 = Math.Abs(Math.Atan(dy1 / dx1)) + Math.PI * 1.0; //3.bölge
                    else if ( centerX < pnt1X && centerY > pnt1Y )
                        alpha1 = Math.Abs(Math.Atan(dx1 / dy1)) + Math.PI * 1.5; //4.bölge
                    else
                    {
                        if ( centerX == pnt1X )
                            if ( centerY < pnt1Y )
                                alpha1 = Math.PI * 0.5;//90c
                            else
                                alpha1 = Math.PI * 1.5; //270c
                        else if ( centerY == pnt1Y )
                            if ( centerX < pnt1X )
                                alpha1 = Math.PI * 0;//0c
                            else
                                alpha1 = Math.PI * 1; //180c
                    }
                    #endregion
                    #region alpha2 region find
                    if ( centerX < pnt2X && centerY < pnt2Y )
                        alpha2 = Math.Abs(Math.Atan(dy2 / dx2)) + Math.PI * 0; //1.bölge
                    else if ( centerX > pnt2X && centerY < pnt2Y )
                        alpha2 = Math.Abs(Math.Atan(dx2 / dy2)) + Math.PI * 0.5; //2.bölge
                    else if ( centerX > pnt2X && centerY > pnt2Y )
                        alpha2 = Math.Abs(Math.Atan(dy2 / dx2)) + Math.PI * 1.0; //3.bölge
                    else if ( centerX < pnt2X && centerY > pnt2Y )
                        alpha2 = Math.Abs(Math.Atan(dx2 / dy2)) + Math.PI * 1.5; //4.bölge
                    else
                    {
                        if ( centerX == pnt2X )
                            if ( centerY < pnt2Y )
                                alpha2 = Math.PI * 0.5;//90c
                            else
                                alpha2 = Math.PI * 1.5; //270c
                        else if ( centerY == pnt2Y )
                            if ( centerX < pnt2X )
                                alpha2 = Math.PI * 0;//0c
                            else
                                alpha2 = Math.PI * 1; //180c
                    }
                    #endregion
                    #region Arc Points Draw
                    if ( alpha1 > alpha2 )
                    {
                        for (
                                double alpha = alpha1;
                                alpha > alpha2;
                                alpha = alpha + ( ( alpha2 - alpha1 ) / 20 )
                            )
                            Points.Add(new Point(Convert.ToInt16(centerX - ( Math.Cos(alpha) * dR1 )), Convert.ToInt16(centerY - ( Math.Sin(alpha) * dR1 ))));
                    }
                    else
                    {
                        for (
                                double alpha = alpha1;
                                alpha < alpha2;
                                alpha = alpha + ( ( alpha2 - alpha1 ) / 20 )
                            )
                            Points.Add(new Point(Convert.ToInt16(centerX + ( Math.Cos(alpha) * dR1 )), Convert.ToInt16(centerY + ( Math.Sin(alpha) * dR1 ))));

                    }
                    #endregion
                }
            }
            else //if ( _geom.IsPath() || _geom.IsArced() )
            {

                object[,] outlinepoints = (object[,])_geom.get_PointsArray(EPcbUnit.epcbUnitMils);


                for ( int i = 0;i < outlinepoints.GetLength(1) - 1;i++ )
                {
                    int pnt1X = Convert.ToInt16(outlinepoints[0, i]);
                    int pnt1Y = Convert.ToInt16(outlinepoints[1, i]);
                    int pnt2X = Convert.ToInt16(outlinepoints[0, i + 1]);
                    int pnt2Y = Convert.ToInt16(outlinepoints[1, i + 1]);

                    if ( pnt1X == pnt2X )
                    {
                        if ( pnt1Y < pnt2Y )
                        {
                            for ( int pnt = pnt1Y;pnt <= pnt2Y;pnt = pnt + 10 )
                            {
                                Points.Add(new Point(pnt1X, pnt));
                            }
                        }
                        if ( pnt2Y < pnt1Y )
                        {
                            for ( int pnt = pnt2Y;pnt <= pnt1Y;pnt = pnt + 10 )
                            {
                                Points.Add(new Point(pnt1X, pnt));
                            }
                        }
                    }
                    else if ( pnt1Y == pnt2Y )
                    {
                        if ( pnt1X < pnt2X )
                        {
                            for ( int pnt = pnt1X;pnt <= pnt2X;pnt = pnt + 10 )
                            {
                                Points.Add(new Point(pnt, pnt1Y));
                            }
                        }
                        if ( pnt2X < pnt1X )
                        {
                            for ( int pnt = pnt2X;pnt <= pnt1X;pnt = pnt + 10 )
                            {
                                Points.Add(new Point(pnt, pnt1Y));
                            }
                        }
                    }
                    else
                    {
                        int Xlenght, Ylenght;
                        Xlenght = pnt2X - pnt1X;
                        Ylenght = pnt2Y - pnt1Y;

                        float Xratio = Xlenght / 10;
                        float Yratio = Ylenght / 10;

                        for ( int h = 0;h < 10;h++ )
                            Points.Add(new Point(Convert.ToInt16(pnt1X + ( h * Xratio )), Convert.ToInt16(pnt1Y + ( h * Yratio ))));


                    }
                }
            }
            return Points;
        }
        
    }
}
