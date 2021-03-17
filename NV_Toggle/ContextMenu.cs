using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace NV_Toggle
{
	class ContextMenu
	{
		bool isAboutLoaded = false;

		public ContextMenuStrip Create()
		{
			//// Add the default menu options.
			ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem item;
            //ToolStripSeparator sep;

            //// Windows Explorer.
            //item = new ToolStripMenuItem();
            //item.Text = "Explorer";
            //item.Click += new EventHandler(Explorer_Click);
            //item.Image = Resources.Explorer;
            //menu.Items.Add(item);

            //// About.
            //item = new ToolStripMenuItem();
            //item.Text = "About";
            //item.Click += new EventHandler(About_Click);
            //item.Image = Resources.About;
            //menu.Items.Add(item);

            //// Separator.
            //sep = new ToolStripSeparator();
            //menu.Items.Add(sep);

            //// Exit.
            item = new ToolStripMenuItem();
            item.Text = "Exit";
            item.Click += new System.EventHandler(Exit_Click);
            menu.Items.Add(item);

            return menu;
		}

		void Explorer_Click(object sender, EventArgs e)
		{
			Process.Start("explorer", null);
		}

		void About_Click(object sender, EventArgs e)
		{
			if (!isAboutLoaded)
			{
				isAboutLoaded = true;
				//new AboutBox().ShowDialog();
				isAboutLoaded = false;
			}
		}

		void Exit_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}
	}
}
