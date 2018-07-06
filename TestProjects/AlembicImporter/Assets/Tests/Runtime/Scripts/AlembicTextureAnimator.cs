using System.Runtime.InteropServices;
using UnityEngine;

namespace UTJ.Alembic
{
    public class AlembicTextureAnimator : MonoBehaviour
    {
        public Material m_targetMaterial;
        public string m_texturePropertyName = "_MainTex";
        public Texture[] m_textures;

        void AbcOnFrameChange(int frame)
        {
            //Debug.Log("AbcOnFrameChange " + frame);
            if (m_targetMaterial != null && m_textures != null && frame >= 0 && frame < m_textures.Length)
            {
                m_targetMaterial.SetTexture(m_texturePropertyName, m_textures[frame]);
            }
        }
    }
}
