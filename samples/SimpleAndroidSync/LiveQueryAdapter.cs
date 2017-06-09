/*
 * Couchbase Lite for .NET
 *
 * Original iOS version by Jens Alfke
 * Android Port by Marty Schoch, Traun Leyden
 * C# Port by Zack Gramana
 *
 * Copyright (c) 2012, 2013, 2014 Couchbase, Inc. All rights reserved.
 * Portions (c) 2013, 2014 Xamarin, Inc. All rights reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file
 * except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software distributed under the
 * License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND,
 * either express or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

using System;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Couchbase.Lite;

using CBDocument = Couchbase.Lite.Document;
using JObject = Java.Lang.Object;

namespace CouchbaseSample.Android.Helper
{
    public class LiveQueryAdapter : BaseAdapter<QueryRow>
    {
        private LiveQuery query;

        protected QueryEnumerator enumerator;

        protected Context Context;

        public event EventHandler<QueryChangeEventArgs> DataSetChanged;

        public LiveQueryAdapter(Context context, LiveQuery query)
        {
            this.Context = context;
            this.query = query;
            query.Changed += (sender, e) => {
                enumerator = e.Rows;
                ((Activity)context).RunOnUiThread(new Action(()=>{
                    NotifyDataSetChanged();
                }));
            };

            //TODO: Revise
            query.Start();
        }

        public override int Count
        {
            get { 
                return enumerator != null ? enumerator.Count : 0; 
            }
        }

        public override QueryRow this[int index]
        {
            get {
                var val = enumerator != null ? enumerator.ElementAt(index) : null;
                return val;
            }
        }

        public override long GetItemId(int position)
        {
            return enumerator.ElementAt(position).SequenceNumber;
        }

        public override global::Android.Views.View GetView(int position, global::Android.Views.View convertView, ViewGroup parent)
        {
            return null;
        }

        public virtual void Invalidate()
        {
            if (query != null)
            {
                query.Stop();
            }
        }
           
    }
}