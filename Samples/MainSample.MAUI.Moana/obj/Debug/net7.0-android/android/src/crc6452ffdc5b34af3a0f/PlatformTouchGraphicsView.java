package crc6452ffdc5b34af3a0f;


public class PlatformTouchGraphicsView
	extends crc643f2b18b2570eaa5a.PlatformGraphicsView
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onLayout:(ZIIII)V:GetOnLayout_ZIIIIHandler\n" +
			"n_onTouchEvent:(Landroid/view/MotionEvent;)Z:GetOnTouchEvent_Landroid_view_MotionEvent_Handler\n" +
			"n_onHoverEvent:(Landroid/view/MotionEvent;)Z:GetOnHoverEvent_Landroid_view_MotionEvent_Handler\n" +
			"";
		mono.android.Runtime.register ("Microsoft.Maui.Platform.PlatformTouchGraphicsView, Microsoft.Maui", PlatformTouchGraphicsView.class, __md_methods);
	}


	public PlatformTouchGraphicsView (android.content.Context p0)
	{
		super (p0);
		if (getClass () == PlatformTouchGraphicsView.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.Platform.PlatformTouchGraphicsView, Microsoft.Maui", "Android.Content.Context, Mono.Android", this, new java.lang.Object[] { p0 });
		}
	}


	public PlatformTouchGraphicsView (android.content.Context p0, android.util.AttributeSet p1)
	{
		super (p0, p1);
		if (getClass () == PlatformTouchGraphicsView.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.Platform.PlatformTouchGraphicsView, Microsoft.Maui", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android", this, new java.lang.Object[] { p0, p1 });
		}
	}


	public PlatformTouchGraphicsView (android.content.Context p0, android.util.AttributeSet p1, int p2)
	{
		super (p0, p1, p2);
		if (getClass () == PlatformTouchGraphicsView.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.Platform.PlatformTouchGraphicsView, Microsoft.Maui", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}


	public PlatformTouchGraphicsView (android.content.Context p0, android.util.AttributeSet p1, int p2, int p3)
	{
		super (p0, p1, p2, p3);
		if (getClass () == PlatformTouchGraphicsView.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.Platform.PlatformTouchGraphicsView, Microsoft.Maui", "Android.Content.Context, Mono.Android:Android.Util.IAttributeSet, Mono.Android:System.Int32, System.Private.CoreLib:System.Int32, System.Private.CoreLib", this, new java.lang.Object[] { p0, p1, p2, p3 });
		}
	}


	public void onLayout (boolean p0, int p1, int p2, int p3, int p4)
	{
		n_onLayout (p0, p1, p2, p3, p4);
	}

	private native void n_onLayout (boolean p0, int p1, int p2, int p3, int p4);


	public boolean onTouchEvent (android.view.MotionEvent p0)
	{
		return n_onTouchEvent (p0);
	}

	private native boolean n_onTouchEvent (android.view.MotionEvent p0);


	public boolean onHoverEvent (android.view.MotionEvent p0)
	{
		return n_onHoverEvent (p0);
	}

	private native boolean n_onHoverEvent (android.view.MotionEvent p0);

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
