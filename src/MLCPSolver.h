#pragma once

#include "MLCPSolverInterface.h"
#include "SequentialImpulseConstraintSolver.h"

namespace BulletSharp
{
	public ref class MLCPSolver : SequentialImpulseConstraintSolver
	{
	internal:
		MLCPSolver(btMLCPSolver* native);

	public:
		MLCPSolver(MLCPSolverInterface^ solver);

		void SetMLCPSolver(MLCPSolverInterface^ solver);

		property btScalar Cfm
		{
			btScalar get();
			void set(btScalar cfm);
		}

		property int NumFallbacks
		{
			int get();
			void set(int num);
		}
	};
};
