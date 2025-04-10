#if NETCORE
using System.Runtime.InteropServices.Marshalling;
using SDL3;

namespace System.Windows.Forms;

public enum MessageBoxButtons
{
	OK,
	OKCancel,
	//AbortRetryIgnore,
	YesNoCancel,
	YesNo,
	RetryCancel
}

public enum MessageBoxIcon : uint
{
	None = 0,
	Error = SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
	Hand = SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
	Stop = SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
	Exclamation = SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING,
	Warning = SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING,
	Asterisk = SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION,
	Information = SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION
}

public enum DialogResult
{
	None,
	OK,
	Cancel,
	Abort,
	Retry,
	Ignore,
	Yes,
	No
}

public static unsafe class MessageBox
{
	private static SDL.SDL_MessageBoxButtonData OKButton = new SDL.SDL_MessageBoxButtonData {
		flags = SDL.SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_RETURNKEY_DEFAULT,
		buttonID = (int)DialogResult.OK,
		text = Utf8StringMarshaller.ConvertToUnmanaged("OK")
	};

	private static SDL.SDL_MessageBoxButtonData CancelButton = new SDL.SDL_MessageBoxButtonData {
		flags = SDL.SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_ESCAPEKEY_DEFAULT,
		buttonID = (int)DialogResult.Cancel,
		text = Utf8StringMarshaller.ConvertToUnmanaged("Cancel")
	};

	private static SDL.SDL_MessageBoxButtonData YesButton = new SDL.SDL_MessageBoxButtonData {
		flags = SDL.SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_RETURNKEY_DEFAULT,
		buttonID = (int)DialogResult.Yes,
		text = Utf8StringMarshaller.ConvertToUnmanaged("Yes")
	};

	private static SDL.SDL_MessageBoxButtonData NoButton = new SDL.SDL_MessageBoxButtonData {
		buttonID = (int)DialogResult.No,
		text = Utf8StringMarshaller.ConvertToUnmanaged("No")
	};

	private static SDL.SDL_MessageBoxButtonData RetryButton = new SDL.SDL_MessageBoxButtonData {
		flags = SDL.SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_RETURNKEY_DEFAULT,
		buttonID = (int)DialogResult.Retry,
		text = Utf8StringMarshaller.ConvertToUnmanaged("Retry")
	};

	private static readonly SDL.SDL_MessageBoxButtonData[] ok = [OKButton];
	private static readonly SDL.SDL_MessageBoxButtonData[] okCancel = [CancelButton, OKButton];
	private static readonly SDL.SDL_MessageBoxButtonData[] yesNo = [NoButton, YesButton];
	private static readonly SDL.SDL_MessageBoxButtonData[] yesNoCancel = [CancelButton, NoButton, YesButton];
	private static readonly SDL.SDL_MessageBoxButtonData[] retryCancel = [CancelButton, RetryButton];

	public static DialogResult Show(string msg, string title, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None)
	{
		var theButtons = buttons switch {
			MessageBoxButtons.OK => ok,
			MessageBoxButtons.OKCancel => okCancel,
			MessageBoxButtons.YesNo => yesNo,
			MessageBoxButtons.YesNoCancel => yesNoCancel,
			MessageBoxButtons.RetryCancel => retryCancel,
			_ => throw new NotImplementedException(),
		};

		fixed (SDL.SDL_MessageBoxButtonData* pButtons = &theButtons[0]) {
			var msgBox = new SDL.SDL_MessageBoxData {
				flags = (SDL.SDL_MessageBoxFlags)icon,
				message = Utf8StringMarshaller.ConvertToUnmanaged(msg),
				title = Utf8StringMarshaller.ConvertToUnmanaged(title),
				buttons = pButtons
			};
			msgBox.numbuttons = theButtons.Length;

			SDL.SDL_ShowMessageBox(ref msgBox, out int buttonid);
			return (DialogResult)buttonid;
		}
	}
}
#endif
