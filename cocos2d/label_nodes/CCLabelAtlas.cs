using System.Diagnostics;
using System;

namespace cocos2d
{
    public class CCLabelAtlas : CCAtlasNode, ICCLabelProtocol
    {
        protected char m_cMapStartChar;
        protected string m_sString = "";

        #region ICCLabelProtocol Members
        public string Label
        {
            get { return m_sString; }
            set 
            {            
                // TODO: Check for null????
                int len = value.Length;
                if (len > m_pTextureAtlas.TotalQuads)
                {
                    m_pTextureAtlas.ResizeCapacity(len);
                }
                
                m_sString = value;
                
                UpdateAtlasValues();
                
                ContentSize = new CCSize(len * m_uItemWidth, m_uItemHeight);
                
                m_uQuadsToDraw = len;

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

        public static CCLabelAtlas Create(string label, string fntFile)
        {
            var pRet = new CCLabelAtlas();
            pRet.InitWithString(label, fntFile);
            return pRet;
        }

        public static CCLabelAtlas Create(string label, string charMapFile, int itemWidth, int itemHeight, char startCharMap)
        {
            var pRet = new CCLabelAtlas();
            pRet.InitWithString(label, charMapFile, itemWidth, itemHeight, startCharMap);
            return pRet;
        }

        public bool InitWithString(string theString, string fntFile)
        {
            string data = CCFileUtils.GetFileData(fntFile);

            PlistDocument doc = PlistDocument.Create(data);
            var dict = doc.Root as PlistDictionary;

            Debug.Assert(dict["version"].AsInt == 1, "Unsupported version. Upgrade cocos2d version");

            string textureFilename = dict["textureFilename"].AsString;
            int width = dict["itemWidth"].AsInt / CCMacros.CCContentScaleFactor();
            int height = dict["itemHeight"].AsInt / CCMacros.CCContentScaleFactor();
            var startChar = (char) dict["firstChar"].AsInt;


            return InitWithString(theString, textureFilename, width, height, startChar);
        }

        public bool InitWithString(string label, string charMapFile, int itemWidth, int itemHeight, char startCharMap)
        {
            Debug.Assert(label != null);
            if (base.InitWithTileFile(charMapFile, itemWidth, itemHeight, label.Length))
            {
                m_cMapStartChar = startCharMap;
                Label = (label);
                return true;
            }
            return false;
        }

        public override void UpdateAtlasValues()
        {
            int n = m_sString.Length;

            CCTexture2D texture = m_pTextureAtlas.Texture;

            float textureWide = texture.PixelsWide;
            float textureHigh = texture.PixelsHigh;

            float itemWidthInPixels = m_uItemWidth * CCMacros.CCContentScaleFactor();
            float itemHeightInPixels = m_uItemHeight * CCMacros.CCContentScaleFactor();

            for (int i = 0; i < n; i++)
            {
                var a = (char) (m_sString[i] - m_cMapStartChar);
                float row = (a % m_uItemsPerRow);
                float col = (a / m_uItemsPerRow);

#if CC_FIX_ARTIFACTS_BY_STRECHING_TEXEL
    // Issue #938. Don't use texStepX & texStepY
            float left		= (2 * row * itemWidthInPixels + 1) / (2 * textureWide);
            float right		= left + (itemWidthInPixels * 2 - 2) / (2 * textureWide);
            float top		= (2 * col * itemHeightInPixels + 1) / (2 * textureHigh);
            float bottom	= top + (itemHeightInPixels * 2 - 2) / (2 * textureHigh);
#else
                float left = row * itemWidthInPixels / textureWide;
                float right = left + itemWidthInPixels / textureWide;
                float top = col * itemHeightInPixels / textureHigh;
                float bottom = top + itemHeightInPixels / textureHigh;
#endif
                // ! CC_FIX_ARTIFACTS_BY_STRECHING_TEXEL

                CCV3F_C4B_T2F_Quad quad;
              
                quad.TopLeft.TexCoords.U = left;
                quad.TopLeft.TexCoords.V = top;
                quad.TopRight.TexCoords.U = right;
                quad.TopRight.TexCoords.V = top;
                quad.BottomLeft.TexCoords.U = left;
                quad.BottomLeft.TexCoords.V = bottom;
                quad.BottomRight.TexCoords.U = right;
                quad.BottomRight.TexCoords.V = bottom;

                quad.BottomLeft.Vertices.X = i * m_uItemWidth;
                quad.BottomLeft.Vertices.Y = 0.0f;
                quad.BottomLeft.Vertices.Z = 0.0f;
                quad.BottomRight.Vertices.X = i * m_uItemWidth + m_uItemWidth;
                quad.BottomRight.Vertices.Y = 0.0f;
                quad.BottomRight.Vertices.Z = 0.0f;
                quad.TopLeft.Vertices.X = i * m_uItemWidth;
                quad.TopLeft.Vertices.Y = m_uItemHeight;
                quad.TopLeft.Vertices.Z = 0.0f;
                quad.TopRight.Vertices.X = i * m_uItemWidth + m_uItemWidth;
                quad.TopRight.Vertices.Y = m_uItemHeight;
                quad.TopRight.Vertices.Z = 0.0f;

                quad.TopLeft.Colors = quad.TopRight.Colors = quad.BottomLeft.Colors = quad.BottomRight.Colors = new CCColor4B(m_tColor.R, m_tColor.G, m_tColor.B, m_cOpacity);

                m_pTextureAtlas.UpdateQuad(ref quad, i);
            }
        }
    }
}