﻿using System.Collections.Generic;

namespace PSExampleApp.Common.Models
{
    /// <summary>
    /// The saved measurement is the measurement that will be saved in the database. It will have
    /// </summary>
    public class SavedMeasurement : DataObject
    {
        /// <summary>
        /// The concentration amount which is calculated based on the found peaks and the concentration method
        /// </summary>
        public double Concentration { get; set; }

        /// <summary>
        /// Gets or sets the configuretion which are set by the heavy metal application
        /// </summary>
        public MeasurementConfiguration Configuration { get; set; }

        /// <summary>
        /// Gets or sets the images that are save as bytearray
        /// </summary>
        public List<byte[]> SaveImages { get; set; }

        /// <summary>
        /// Gets or sets the serialized simple measurement with data containing from the palmsense core measurement. This part is serialized to byte array and can be deserialized using the LoadSaveHelper functions.
        /// </summary>
        public byte[] SerializedMeasurement { get; set; }
    }
}