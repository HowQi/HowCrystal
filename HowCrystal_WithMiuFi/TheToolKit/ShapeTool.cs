using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace TheToolKit
{
    public static class ShapeTool
    {
        //=========================获取内部矩形相关============================//

        /// <summary>
        /// 计算将一个矩形嵌套进另一个矩形后,该矩形的大小变化、位置变化。
        /// </summary>
        /// <param name="outer">作为容器的外部矩形</param>
        /// <param name="innerRaw">内部矩形原尺寸</param>
        /// <param name="offset">嵌套后,内部矩形为了居中相对于外矩形偏移量</param>
        /// <param name="innerRes">嵌套后,内部矩形的尺寸</param>
        public static void CalcuInnerCenterRectangleF(SizeF outer, SizeF innerRaw, out SizeF offset, out SizeF innerRes)
        {
            //如果其中包含了0尺寸,返回0大小
            if (outer.Width * outer.Height * innerRaw.Width * innerRaw.Height == 0f)
            {
                offset = outer / 2;
                innerRes = new SizeF(0, 0);
            }
            double outerRito = outer.Width / outer.Height;
            double innerRito = innerRaw.Width / innerRaw.Height;

            if (outerRito > innerRito)
            {
                innerRes = new SizeF((float)(outer.Height * innerRito), outer.Height);
                offset = new SizeF((outer.Width - innerRes.Width) / 2, 0);
            }
            else if (outerRito < innerRito)
            {
                innerRes = new SizeF(outer.Width, (float)(outer.Width / innerRito));
                offset = new SizeF(0, (outer.Height - innerRes.Height) / 2);
            }
            else//相等的情况
            {
                innerRes = outer;
                offset = new SizeF(0, 0);
            }
        }

        public static void CalcuInnerCenterRectangle(Size outer, Size innerRaw, out Size offset, out Size innerRes)
        {
            SizeF offsetF;
            SizeF innerResF;
            CalcuInnerCenterRectangleF(new SizeF(outer), new SizeF(innerRaw), out offsetF, out innerResF);
            offset = offsetF.ToSize();
            innerRes = innerResF.ToSize();
        }

        public static RectangleF CalcuInnerCenterRectangleF(RectangleF outer, SizeF inner)
        {
            SizeF ofst, rectSz;
            CalcuInnerCenterRectangleF(outer.Size, inner, out ofst, out rectSz);
            return new RectangleF(
                outer.Location + ofst,
                rectSz
                );
        }

        public static Rectangle CalcuInnerCenterRectangle(Rectangle outer, Size inner)
        {
            Size ofst, rectSz;
            CalcuInnerCenterRectangle(outer.Size, inner, out ofst, out rectSz);
            return new Rectangle(
                outer.Location + ofst,
                rectSz
                );
        }

        public static RectangleF GetInnerRectF(this RectangleF lval,SizeF inner)
        {
            return CalcuInnerCenterRectangleF(lval, inner);
        }

        public static Rectangle GetInnerRect(this Rectangle lval,Size inner)
        {
            return CalcuInnerCenterRectangle(lval, inner);
        }



    }
}
