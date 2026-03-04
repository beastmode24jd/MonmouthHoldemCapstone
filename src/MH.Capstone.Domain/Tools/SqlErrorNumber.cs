using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MH.Capstone.Domain.Tools
{
    internal enum SqlErrorNumber
    {
        UniqueConstraintViolation = 2627,
        ForeignKeyConstraintViolation = 547,
        CheckConstraintViolation = 547,
        PrimaryKeyConstraintViolation = 2627,
        DeadlockVictim = 1205,
        TimeoutExpired = -2
    }
}
