/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Model;
using System.Reflection;
using TSO.Common.rendering.framework.model;

namespace TSOClient.Code.UI.Framework
{
    /// <summary>
    /// Utility for performing animations on UIElements
    /// </summary>
    public class UITween : IUIProcess
    {
        private List<UITweenInstance> m_ActiveTweens = new List<UITweenInstance>();

        public UITweenInstance To(object obj, float duration, Dictionary<string, float> args)
        {
            return To(obj, duration, args, TweenLinear.EaseNone);
        }

        public UITweenInstance To(object obj, float duration, Dictionary<string, float> args, EaseFunction ease)
        {
            var inst = new UITweenInstance(this, obj, duration, args, ease);
            lock (m_ActiveTweens)
            {
                m_ActiveTweens.Add(inst);
            }
            inst.Start();
            return inst;
        }

        public void Stop(UITweenInstance inst, bool complete)
        {
            if (complete)
            {
                inst.RenderPercent(1.0f);
            }
            lock (m_ActiveTweens)
            {
                m_ActiveTweens.Remove(inst);
            }
        }


        #region IUIProcess Members

        public void Update(UpdateState state)
        {

            var done = new List<UITweenInstance>();

            lock (m_ActiveTweens)
            {
                if (m_ActiveTweens.Count == 0) { return; }

                var now = state.Time.TotalGameTime.Ticks;

                var copy = m_ActiveTweens.ToList();
                foreach (var tween in copy)
                {
                    tween.Update(now, state);
                    if (!tween.Active)
                    {
                        /** Done **/
                        done.Add(tween);
                    }
                }
            }

            foreach (var tween in done)
            {
                tween.Complete();
            }
        }

        #endregion
    }

    public delegate void TweenEvent(UITweenInstance tween, float progress);

    public class UITweenInstance
    {
        private List<UITweenInstanceField> m_Fields;
        private object m_Object;
        private float m_Duration;
        private float m_StartTime;
        private bool m_Active;
        private EaseFunction m_EaseFunction;
        private UITween m_Owner;
        private float m_LastProgress;


        public event TweenEvent OnComplete;
        


        public UITweenInstance(UITween owner, object obj, float duration, Dictionary<string, float> args, EaseFunction ease)
        {
            this.m_Owner = owner;
            this.m_EaseFunction = ease;
            this.m_Object = obj;
            /** Convert secs into ticks **/
            this.m_Duration = new TimeSpan(0, 0, 0, 0, (int)(duration * 1000.0f)).Ticks;

            var clazz = obj.GetType();

            m_Fields = new List<UITweenInstanceField>();
            foreach (var key in args.Keys)
            {
                var prop = clazz.GetMember(key)[0];
                if(prop is FieldInfo){
                    m_Fields.Add(new UITweenInstanceField_ForField(prop as FieldInfo, obj)
                    {
                        End = (float)args[key]
                    });
                }
                else if (prop is PropertyInfo)
                {
                    m_Fields.Add(new UITweenInstanceField_ForProperty(prop as PropertyInfo, obj)
                    {
                        End = (float)args[key]
                    });
                }
            }
        }

        public void Start()
        {
            m_StartTime = GameFacade.GameRunTime.Ticks;
            m_Active = true;
        }

        public void Complete()
        {
            if (LastProgress != 1.0f)
            {
                RenderPercent(1.0f);
            }
            if (OnComplete != null)
            {
                OnComplete(this, 1.0f);
            }
            m_Owner.Stop(this, false);
        }


        public void Update(long ticks, UpdateState state)
        {
            if (!m_Active) { return; }

            var time = ticks - m_StartTime;
            var progress = (time) / m_Duration;
            if (progress >= 1)
            {
                progress = 1;
            }
            else if (progress < 0)
            {
                progress = 0;
            }
            else
            {
                progress = m_EaseFunction(time, 0, 1, m_Duration);
            }
            m_LastProgress = progress;
            RenderPercent(progress);
        }


        public void RenderPercent(float progress)
        {
            foreach (var field in m_Fields)
            {
                field.SetValue(field.Start + ((field.End - field.Start) * progress), m_Object);
            }

            if (progress >= 1.0f)
            {
                m_Active = false;
                m_Owner.Stop(this, false);
            }
        }

        public bool Active
        {
            get
            {
                return m_Active;
            }
        }

        public float LastProgress
        {
            get
            {
                return m_LastProgress;
            }
        }
    }


    public abstract class UITweenInstanceField
    {
        public float Start;
        public float End;

        public abstract void SetValue(object value, object inst);
    }


    public class UITweenInstanceField_ForProperty : UITweenInstanceField
    {
        public PropertyInfo Property;

        public UITweenInstanceField_ForProperty(PropertyInfo property, object inst)
        {
            this.Property = property;
            Start = (float)GetValue(inst);
        }


        public object GetValue(object inst)
        {
            return Property.GetValue(inst, null);
        }

        public override void SetValue(object value, object inst)
        {
            Property.SetValue(inst, value, null);
        }
    }

    public class UITweenInstanceField_ForField : UITweenInstanceField
    {
        public FieldInfo Field;

        public UITweenInstanceField_ForField(FieldInfo field, object inst)
        {
            this.Field = field;
            Start = (float)GetValue(inst);
        }


        public object GetValue(object inst)
        {
            return Field.GetValue(inst);
        }

        public override void SetValue(object value, object inst)
        {
            Field.SetValue(inst, value);
        }
    }





    public delegate float EaseFunction(float time, float min, float max, float duration);


    #region Easing Functions

    public class TweenLinear
    {
        public static EaseFunction EaseNone = new EaseFunction(_EaseNone);

        private static float _EaseNone(float t, float b, float c, float d){
            return c*t/d + b;
        }
    }

    public class TweenQuad
    {
        public static EaseFunction EaseIn = new EaseFunction(_EaseIn);
        public static EaseFunction EaseOut = new EaseFunction(_EaseOut);
        public static EaseFunction EaseInOut = new EaseFunction(_EaseInOut);

        private static float _EaseIn(float t, float b, float c, float d)
        {
            return c * (t /= d) * t + b;
        }

        private static float _EaseOut(float t, float b, float c, float d)
        {
            return -c * (t /= d) * (t - 2) + b;
        }

        private static float _EaseInOut(float t, float b, float c, float d)
        {
            if ((t /= d * 0.5f) < 1) return c * 0.5f * t * t + b;
            return -c * 0.5f * ((--t) * (t - 2) - 1) + b;
        }
    }

    public class TweenElastic
    {
        public static EaseFunction EaseIn = new EaseFunction(_EaseIn);
        //public static EaseFunction EaseOut = new EaseFunction(_EaseOut);
        //public static EaseFunction EaseInOut = new EaseFunction(_EaseInOut);

        private static float _2PI = (float)Math.PI * 2.0f;

        private static float _EaseIn(float t, float b, float c, float d)
        {
            if(t == 0){
                return b;
            }
            if ((t /= d) == 1)
            {
                return b + c;
            }
            var p = d * 0.3f;
            var a = c;
            var s = p / 4.0f;

            return -(a * (float)Math.Pow(2.0f, 10.0f * (t -= 1)) * (float)Math.Sin((t * d - s) * _2PI / p)) + b;
        }

        //private static float _EaseOut(float t, float b, float c, float d)
        //{
        //    if (t == 0)
        //    {
        //        return b;
        //    }
        //    if ((t /= d) == 1)
        //    {
        //        return b + c;
        //    }
        //    var p = d * 0.3f;
        //    var a = c;
        //    var s = p / 4.0f;

        //    return (a * (float)Math.Pow(2.0f, -10.0f * t) * (float)Math.Sin((t * d - s) * _2PI / p) + c + b);
        //}

        //private static float _EaseInOut(float t, float b, float c, float d)
        //{
        //    if ((t /= d * 0.5f) < 1) return c * 0.5f * t * t + b;
        //    return -c * 0.5f * ((--t) * (t - 2) - 1) + b;
        //}
    }



    #endregion
}
