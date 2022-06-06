﻿using System;
using System.Collections.Generic;
using Arrowgene.Ddon.Shared.Entity.Structure;

namespace Arrowgene.Ddon.Shared.Model
{
    public class Character
    {
        public Character()
        {
            Visual = new CDataEditInfo();
            Status = new CDataStatusInfo();
            CustomSkills = new List<CDataSetAcquirementParam>();
        }

        public uint Id { get; set; }
        public int AccountId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime Created { get; set; }
        public CDataEditInfo Visual { get; set; }
        public CDataStatusInfo Status { get; set; }
        public List<CDataSetAcquirementParam> CustomSkills { get; set; }
    }
}
