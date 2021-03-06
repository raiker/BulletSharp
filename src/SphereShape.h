#pragma once

#include "ConvexInternalShape.h"

namespace BulletSharp
{
	public ref class SphereShape : ConvexInternalShape
	{
	internal:
		SphereShape(btSphereShape* native);

	public:
		SphereShape(btScalar radius);

		void SetUnscaledRadius(btScalar radius);

		property btScalar Radius
		{
			btScalar get();
		}
	};
};
