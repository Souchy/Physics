using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Test.Arch;

public record struct Position(Vector2 Value);
public record struct Velocity(Vector2 Value);