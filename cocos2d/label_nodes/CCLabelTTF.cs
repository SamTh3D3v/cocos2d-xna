using Microsoft.Xna.Framework.Graphics;
using System;

namespace cocos2d
{
    public class CCLabelTTF : CCSprite, ICCLabelProtocol
    {
        private float m_fFontSize;
        private CCTextAlignment m_hAlignment;
        private string m_pFontName;
        protected string m_pString;
        private CCSize m_tDimensions;
        private CCVerticalTextAlignment m_vAlignment;

        public CCLabelTTF ()
        {
            m_hAlignment = CCTextAlignment.CCTextAlignmentCenter;
            m_vAlignment = CCVerticalTextAlignment.CCVerticalTextAlignmentTop;
            m_pFontName = string.Empty;
            m_fFontSize = 0.0f;
            m_pString = string.Empty;

            Init();
        }
        
        public CCLabelTTF (string text, string fontName, float fontSize) : 
            this (text, fontName, fontSize, CCSize.Zero, CCTextAlignment.CCTextAlignmentCenter,
                  CCVerticalTextAlignment.CCVerticalTextAlignmentTop)
        { }
        
        public CCLabelTTF (string text, string fontName, float fontSize, CCSize dimensions, CCTextAlignment hAlignment) :
            this (text, fontName, fontSize, dimensions, hAlignment, CCVerticalTextAlignment.CCVerticalTextAlignmentTop)
        { }
        
        public CCLabelTTF (string text, string fontName, float fontSize, CCSize dimensions, CCTextAlignment hAlignment,
                           CCVerticalTextAlignment vAlignment)
        {
            InitWithString(text, fontName, fontSize, dimensions, hAlignment, vAlignment);
        }

        public string FontName
        {
            get { return m_pFontName; }
            set
            {
                if (m_pFontName != value)
                {
                    m_pFontName = value;
                    if (m_pString.Length > 0)
                    {
                        Refresh();
                    }
                }
            }
        }

        public float FontSize
        {
            get { return m_fFontSize; }
            set
            {
                if (m_fFontSize != value)
                {
                    m_fFontSize = value;
                    if (m_pString.Length > 0)
                    {
                        Refresh();
                    }
                }
            }
        }

        public CCSize Dimensions
        {
            get { return m_tDimensions; }
            set
            {
                if (!m_tDimensions.Equals(value))
                {
                    m_tDimensions = value;
                    if (m_pString.Length > 0)
                    {
                        Refresh();
                    }
                }
            }
        }

        public CCVerticalTextAlignment VerticalAlignment
        {
            get { return m_vAlignment; }
            set
            {
                if (m_vAlignment != value)
                {
                    m_vAlignment = value;
                    if (m_pString.Length > 0)
                    {
                        Refresh();
                    }
                }
            }
        }

        public CCTextAlignment HorizontalAlignment
        {
            get { return m_hAlignment; }
            set
            {
                if (m_hAlignment != value)
                {
                    m_hAlignment = value;
                    if (m_pString.Length > 0)
                    {
                        Refresh();
                    }
                }
                    }
                }

        internal void Refresh()
        {
            //
            // This can only happen when the frame buffer is ready...
            //
            try
            {
                updateTexture();
                Dirty = false;
            }
            catch (Exception)
            {
            }
        }

        #region ICCLabelProtocol Members

/*
 * This is where the texture should be created, but it messes with the drawing 
 * of the object tree
 * 
        public override void Draw()
        {
            if (Dirty)
            {
                updateTexture();
                Dirty = false;
            }
            base.Draw();
        }
*/
        public string Label
        {
            get { return m_pString; }
            set
            {
                // This is called in the update() call, so it should not do any drawing ...
                if (m_pString != value)
                {
                    m_pString = value;
                    updateTexture();
                    Dirty = false;
                }
                //            Dirty = true;
            }
        }

        [Obsolete("Use Label Property")]
        public void SetString(string label)
        {
            Label = label;
        }
        
        [Obsolete("Use Label Property")]
        public string GetString() 
        {
            return Label;
        }

        #endregion

        public override string ToString()
        {
            return string.Format("FontName:{0}, FontSize:{1}", m_pFontName, m_fFontSize);
        }

        public override bool Init()
        {
            return InitWithString("", "Helvetica", 12);
        }

        public bool InitWithString(string label, string fontName, float fontSize, CCSize dimensions, CCTextAlignment alignment)
        {
            return InitWithString(label, fontName, fontSize, dimensions, alignment, CCVerticalTextAlignment.CCVerticalTextAlignmentTop);
        }

        public bool InitWithString(string label, string fontName, float fontSize)
        {
            return InitWithString(label, fontName, fontSize, CCSize.Zero, CCTextAlignment.CCTextAlignmentLeft,
                                  CCVerticalTextAlignment.CCVerticalTextAlignmentTop);
        }


        public bool InitWithString(string text, string fontName, float fontSize,
                                   CCSize dimensions, CCTextAlignment hAlignment,
                                   CCVerticalTextAlignment vAlignment)
        {
            if (base.Init())
            {
                // shader program
                //this->setShaderProgram(CCShaderCache::sharedShaderCache()->programForKey(SHADER_PROGRAM));

                m_tDimensions = new CCSize(dimensions.Width, dimensions.Height);
                m_hAlignment = hAlignment;
                m_vAlignment = vAlignment;
                m_pFontName = fontName;
                m_fFontSize = fontSize;

                Label = (text);

                return true;
            }

            return false;
        }

        private void updateTexture()
        {
            CCTexture2D tex;

            // Dump the old one
            if (Texture != null)
            {
                Texture.Dispose();
            }

            // let system compute label's width or height when its value is 0
            // refer to cocos2d-x issue #1430
            tex = new CCTexture2D();

            tex.InitWithString(m_pString,
                               CCMacros.CCSizePointsToPixels(m_tDimensions),
                               m_hAlignment,
                               m_vAlignment,
                               m_pFontName,
                               m_fFontSize * CCMacros.CCContentScaleFactor());

            Texture = tex;

            CCRect rect = CCRect.Zero;
            rect.Size = m_pobTexture.ContentSize;
            SetTextureRect(rect);
        }
    }
}