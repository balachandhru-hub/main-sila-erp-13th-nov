import React, { useState } from "react";
import { PageHeader } from "@vosox/shared-ui";
import type { SilaRecipeMasterKind } from "../../../api/silaMe/silaRecipeToolsApi";
import SilaGoodsIssueCostCenters from "./SilaGoodsIssueCostCenters";
import SilaRecipeMasterTab from "./SilaRecipeMasterTab";
import "../silaMeTheme.css";
import "./SilaRecipes.css";

interface SilaRecipeMasterDataProps {
  /** Create, edit, delete and Excel import (MANAGE_SILA_MASTER_DATA); viewing and export need VIEW_SILA_RECIPE. */
  canManage: boolean;
}

type MasterTab = SilaRecipeMasterKind | "goodsIssueCostCenters";

const TABS: { key: MasterTab; label: string }[] = [
  { key: "families", label: "Families" },
  { key: "categories", label: "Categories" },
  { key: "goodsIssueCostCenters", label: "Goods Issue Cost Centers" },
];

/** Recipe master / setup: families, categories, and Goods Issue cost center mappings. */
const SilaRecipeMasterData: React.FC<SilaRecipeMasterDataProps> = ({ canManage }) => {
  const [tab, setTab] = useState<MasterTab>("families");

  return (
    <div className="sila-me srec-page">
      <PageHeader className="pud-page-header" title="Recipe master data" />
      <section className="sila-card">
        <div className="sila-tabs" role="tablist" aria-label="Recipe master data">
          {TABS.map((item) => (
            <button
              key={item.key}
              type="button"
              role="tab"
              id={`srec-tab-${item.key}`}
              className="sila-tab"
              aria-selected={tab === item.key}
              aria-controls={`srec-panel-${item.key}`}
              onClick={() => setTab(item.key)}
            >
              {item.label}
            </button>
          ))}
        </div>
        <div role="tabpanel" id={`srec-panel-${tab}`} aria-labelledby={`srec-tab-${tab}`}>
          {tab === "goodsIssueCostCenters" ? (
            <SilaGoodsIssueCostCenters canManage={canManage} />
          ) : (
            <SilaRecipeMasterTab key={tab} kind={tab} canManage={canManage} />
          )}
        </div>
      </section>
    </div>
  );
};

export default SilaRecipeMasterData;
