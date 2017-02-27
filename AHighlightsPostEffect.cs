using UnityEngine;
using System.Collections.Generic;

using UnityStandardAssets.ImageEffects;
using UnityEngine.Rendering;

[RequireComponent(typeof(BlurOptimized))]
[RequireComponent(typeof(Camera))]
public class AHighlightsPostEffect : MonoBehaviour 
{
	#region enums
	public enum RTResolution
	{
		Quarter = 4,
		Half = 2,
		Full = 1
	}
	#endregion
	
	#region public vars
	public RTResolution m_resolution = RTResolution.Full;
	public static List<Renderer> glows = new List<Renderer> ();
	public static List<Renderer> glows2 = new List<Renderer> ();
	public static List<Renderer> glows3 = new List<Renderer> ();

	public Shader m_highlightShader;

	bool didRender = false;
	
	#endregion
	
	#region private field
	
	private BlurOptimized m_blur;
	
	
	private Material m_highlightMaterial;
	
	private CommandBuffer m_renderBuffer;
	private RenderTexture m_source = null;
	private RenderTexture m_dest = null;
	
	private int m_RTWidth = 512;
	private int m_RTHeight = 512;
	
	#endregion
	
	private void Awake()
	{
		CreateBuffers();
		CreateMaterials();
		
		m_blur = GetComponent<BlurOptimized>();
		m_blur.enabled = false;
		
		m_RTWidth = (int) (Screen.width / (float) m_resolution);
		m_RTHeight = (int) (Screen.height / (float) m_resolution);
	}
	
	private void CreateBuffers()
	{
		m_renderBuffer = new CommandBuffer();
	}
	
	private void ClearCommandBuffers()
	{
		m_renderBuffer.Clear();
	}
	
	private void CreateMaterials()
	{
		m_highlightMaterial = new Material( m_highlightShader );
	}
	
	private void RenderHighlights2( RenderTexture rt)
	{
		bool didRender2 = false;
		RenderTargetIdentifier rtid = new RenderTargetIdentifier(rt);
		m_renderBuffer.SetRenderTarget( rtid );
		RenderTexture.active = rt;
		for(int i = glows.Count - 1; i >= 0; i -= 1)
		{
			if (glows[i].gameObject.activeInHierarchy && glows[i].enabled && glows[i].isVisible){
				ClearCommandBuffers();
				m_highlightMaterial.SetColor("_Color", glows[i].GetComponent<ObjInfo>().glowColor);
				m_renderBuffer.DrawRenderer( glows[i], m_highlightMaterial, 0, 4 );
				Graphics.ExecuteCommandBuffer(m_renderBuffer);
				didRender2 = true;
			}else{
				glows[i].GetComponent<ObjInfo>().addedToGlow = false;
				glows.Remove (glows[i]);
			}
		}
		if (didRender2) {
			m_blur.OnRenderImage (rt, rt);
			m_highlightMaterial.SetTexture ("_OccludeMap", rt);
			Graphics.Blit (m_dest, m_dest, m_highlightMaterial, 5);
			didRender = true;
		}
	}
	
	private void RenderHighlights3(RenderTexture rt){
		bool didRender2 = false;
		RenderTexture rt2 = new RenderTexture (Screen.width, Screen.height, 0);
		ClearTex2 (rt2);
		RenderTargetIdentifier rtid = new RenderTargetIdentifier(rt);
		m_renderBuffer.SetRenderTarget( rtid );
		for(int i = glows2.Count - 1; i >= 0; i -= 1)
		{
			if (glows2[i].gameObject.activeInHierarchy && glows2[i].enabled && glows2[i].isVisible){
				ClearCommandBuffers();
				m_highlightMaterial.SetColor("_Color", glows2[i].GetComponent<ObjInfo>().glowColor);
				m_renderBuffer.DrawRenderer( glows2[i], m_highlightMaterial, 0, 4 );
				Graphics.ExecuteCommandBuffer(m_renderBuffer);
				didRender2 = true;
			}else{
				glows2[i].GetComponent<ObjInfo>().addedToGlow = false;
				glows2.Remove(glows2[i]);
			}
		}
		if (didRender2) {
			m_blur.OnRenderImage (rt2, rt);
			m_highlightMaterial.SetTexture ("_OccludeMap", rt2);
			Graphics.Blit (rt, rt, m_highlightMaterial, 2);
			m_highlightMaterial.SetTexture ("_OccludeMap", rt);
			Graphics.Blit (m_dest, m_dest, m_highlightMaterial, 5);
			didRender = true;
		}
		rt2.Release ();
	}
	
	private void RenderHighlights4(RenderTexture rt){
		RenderTexture rt2 = new RenderTexture (Screen.width, Screen.height, 0);
		RenderTargetIdentifier rtid = new RenderTargetIdentifier(rt2);
		m_renderBuffer.SetRenderTarget( rtid );
		for(int i = glows3.Count - 1; i >= 0; i -= 1)
		{
			if (glows3[i].gameObject.activeInHierarchy && glows3[i].enabled && glows3[i].isVisible){
				ClearTex2 (rt);
				ClearTex2 (rt2);
				ClearCommandBuffers();
				m_highlightMaterial.SetColor("_Color", glows3[i].GetComponent<ObjInfo>().glowColor);
				m_renderBuffer.DrawRenderer( glows3[i], m_highlightMaterial, 0, 4 );
				Graphics.ExecuteCommandBuffer(m_renderBuffer);
				m_blur.OnRenderImage (rt2, rt);
				m_highlightMaterial.SetTexture ("_OccludeMap", rt2);
				Graphics.Blit (rt, rt, m_highlightMaterial, 2);
				m_highlightMaterial.SetTexture ("_OccludeMap", rt);
				Graphics.Blit (m_dest, m_dest, m_highlightMaterial, 5);
				didRender = true;
			}else{
				glows3[i].GetComponent<ObjInfo>().addedToGlow = false;
				glows3.Remove (glows3[i]);
			}
		}
		rt2.Release ();
	}
	
	/// Final image composing.
	/// 1. Renders all the highlight objects either with Overlay shader or DepthFilter
	/// 2. Downsamples and blurs the result image using standard BlurOptimized image effect
	/// 3. Renders occluders to the same render texture
	/// 4. Substracts the occlusion map from the blurred image, leaving the highlight area
	/// 5. Renders the result image over the main camera's G-Buffer
	/// 
	
	private void ClearTex(RenderTexture rt){
		ClearTex2 (rt);
		RenderTexture.active = null;
	}
	
	private void ClearTex2(RenderTexture rt){
		RenderTexture.active = rt;
		GL.Clear (true, true, Color.clear);
	}
	
	private void OnRenderImage( RenderTexture source, RenderTexture destination )
	{
		if (glows.Count > 0 || glows2.Count > 0 || glows3.Count > 0) {
		
		m_dest = new RenderTexture (Screen.width, Screen.height, 0);
		ClearTex2(m_dest);
		didRender = false;
		RenderTexture highlightRTPart = new RenderTexture (Screen.width, Screen.height, 0);
		//render glow 4
		if (glows3.Count > 0) {
			ClearTex2 (highlightRTPart);
			RenderHighlights4 (highlightRTPart);
		}
		
		//render glow 3
		if (glows2.Count > 0) {
			ClearTex2 (highlightRTPart);
			RenderHighlights3 (highlightRTPart);
		}
		
		//render glow 2
		if (glows.Count > 0){
			ClearTex2 (highlightRTPart);
			RenderHighlights2 (highlightRTPart);
		}
		RenderTexture.active = null;
		highlightRTPart.Release ();
		if (didRender) {
			m_highlightMaterial.SetTexture ("_OccludeMap", m_dest);
			Graphics.Blit (source, m_dest, m_highlightMaterial, 0);
			if (GetComponent<BloomOptimized>()){
				GetComponent<BloomOptimized> ().OnRenderImage (m_dest, destination);
			}else{
				Graphics.Blit (m_dest, destination);
			}
		} else {
			if (GetComponent<BloomOptimized>()){
				GetComponent<BloomOptimized> ().OnRenderImage (source, destination);
			}else{
				Graphics.Blit (source, destination);
			}
		}
		m_dest.Release ();
		} else {
			if (GetComponent<BloomOptimized>()){
				GetComponent<BloomOptimized> ().OnRenderImage (source, destination);
			}else{
				Graphics.Blit (source, destination);
			}
		}
	}
}
