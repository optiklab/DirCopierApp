/*
 * COPYRIGHT 2016 Anton Yarkov
 * 
 * Email: anton.yarkov@gmail.com
 * 
 */
using System;

namespace DirectoryCopierLib
{
    public class ActionHappenedEventArgs : EventArgs
    {
        public ActionHappenedEventArgs(string log)
        {
            Log = log;
        }
        
        public string Log
        {
            get;
            private set;
        }
    }

    public delegate void ActionHappenedEventHandler(Object sender, ActionHappenedEventArgs e);
}
