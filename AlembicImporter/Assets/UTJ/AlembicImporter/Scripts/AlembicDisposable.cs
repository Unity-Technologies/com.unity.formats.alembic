using System;

namespace UTJ.Alembic
{
	public class AlembicDisposable : IDisposable
	{
		protected bool m_Disposed = false;

		public void Dispose()
		{
			DoDispose(true);
		}

		~AlembicDisposable()
		{
			DoDispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{

		}

		private void DoDispose(bool disposing)
		{
			if (m_Disposed)
				return;
			Dispose(disposing);
			m_Disposed = true;
		}
	}
}
