package crc640ec207abc449b2ca;


public class ShellSectionRenderer_ViewPagerPageChanged
	extends androidx.viewpager2.widget.ViewPager2.OnPageChangeCallback
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onPageSelected:(I)V:GetOnPageSelected_IHandler\n" +
			"";
		mono.android.Runtime.register ("Microsoft.Maui.Controls.Platform.Compatibility.ShellSectionRenderer+ViewPagerPageChanged, Microsoft.Maui.Controls", ShellSectionRenderer_ViewPagerPageChanged.class, __md_methods);
	}


	public ShellSectionRenderer_ViewPagerPageChanged ()
	{
		super ();
		if (getClass () == ShellSectionRenderer_ViewPagerPageChanged.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.Controls.Platform.Compatibility.ShellSectionRenderer+ViewPagerPageChanged, Microsoft.Maui.Controls", "", this, new java.lang.Object[] {  });
		}
	}

	public ShellSectionRenderer_ViewPagerPageChanged (crc640ec207abc449b2ca.ShellSectionRenderer p0)
	{
		super ();
		if (getClass () == ShellSectionRenderer_ViewPagerPageChanged.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.Controls.Platform.Compatibility.ShellSectionRenderer+ViewPagerPageChanged, Microsoft.Maui.Controls", "Microsoft.Maui.Controls.Platform.Compatibility.ShellSectionRenderer, Microsoft.Maui.Controls", this, new java.lang.Object[] { p0 });
		}
	}


	public void onPageSelected (int p0)
	{
		n_onPageSelected (p0);
	}

	private native void n_onPageSelected (int p0);

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
