package crc64d693e2d9159537db;


public class BlazorWebChromeClient
	extends android.webkit.WebChromeClient
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onCreateWindow:(Landroid/webkit/WebView;ZZLandroid/os/Message;)Z:GetOnCreateWindow_Landroid_webkit_WebView_ZZLandroid_os_Message_Handler\n" +
			"n_onShowFileChooser:(Landroid/webkit/WebView;Landroid/webkit/ValueCallback;Landroid/webkit/WebChromeClient$FileChooserParams;)Z:GetOnShowFileChooser_Landroid_webkit_WebView_Landroid_webkit_ValueCallback_Landroid_webkit_WebChromeClient_FileChooserParams_Handler\n" +
			"";
		mono.android.Runtime.register ("Microsoft.AspNetCore.Components.WebView.Maui.BlazorWebChromeClient, Microsoft.AspNetCore.Components.WebView.Maui", BlazorWebChromeClient.class, __md_methods);
	}


	public BlazorWebChromeClient ()
	{
		super ();
		if (getClass () == BlazorWebChromeClient.class) {
			mono.android.TypeManager.Activate ("Microsoft.AspNetCore.Components.WebView.Maui.BlazorWebChromeClient, Microsoft.AspNetCore.Components.WebView.Maui", "", this, new java.lang.Object[] {  });
		}
	}


	public boolean onCreateWindow (android.webkit.WebView p0, boolean p1, boolean p2, android.os.Message p3)
	{
		return n_onCreateWindow (p0, p1, p2, p3);
	}

	private native boolean n_onCreateWindow (android.webkit.WebView p0, boolean p1, boolean p2, android.os.Message p3);


	public boolean onShowFileChooser (android.webkit.WebView p0, android.webkit.ValueCallback p1, android.webkit.WebChromeClient.FileChooserParams p2)
	{
		return n_onShowFileChooser (p0, p1, p2);
	}

	private native boolean n_onShowFileChooser (android.webkit.WebView p0, android.webkit.ValueCallback p1, android.webkit.WebChromeClient.FileChooserParams p2);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
