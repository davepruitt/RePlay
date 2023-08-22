using System;

namespace FitMiAndroid
{
    public enum HidPuckCommands
    {
        RBLINK = 0x01,
        GBLINK = 0x02,
        BBLINK = 0x03,
        MBLINK = 0x04,
        RPULSE = 0x05,
        GPULSE = 0x06,
        BPULSE = 0x07,
        MPULSE = 0x08,
        MPUENBL = 0x09,
        PWR = 0x0A,
        GAMEON = 0x0B,
        MAGCALX = 0x0C,
        MAGCALY = 0x0D,
        MAGCALZ = 0x0E,
        DNGLRST = 0x0F,
        SENDVEL = 0x10,
        TOUCHBUZ = 0x11,
        CHANGEFREQ = 0x12,
        RXCHANGEFREQ = 0x13,
        CHANSPY = 0x14,
        SETUSBPIPES = 0x15
    }
}
