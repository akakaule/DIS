import React, { useEffect } from "react";
import { makeStyles } from "@material-ui/core/styles";
import Table from "@material-ui/core/Table";
import TableBody from "@material-ui/core/TableBody";
import TableContainer from "@material-ui/core/TableContainer";
import TablePagination from "@material-ui/core/TablePagination";
import TableRow from "@material-ui/core/TableRow";

import Paper from "@material-ui/core/Paper";
import TableCell from "@material-ui/core/TableCell";
import DataTableHead, { ITableHeadCell } from "./data-table-header";
import Loading from "components/loading/loading";
import { Link, Navigate, useNavigate } from "react-router-dom";
import DataTableToolbar from "./data-table-toolbar";
import { Flex, Text } from "@chakra-ui/react";
import { DataTableCheckbox } from "./data-table-checkbox";
import Button from "components/button";
import { Tooltip, withStyles } from "@material-ui/core";

const useStyles = makeStyles((theme) => ({
  root: {
    width: "100%",
  },
  paper: {
    width: "100%",
    marginBottom: theme.spacing(2),
    display: "flex",
    flexDirection: "column",
    boxShadow: "0 0 black",
  },
  table: {
    minWidth: 750,
  },
  visuallyHidden: {
    border: 0,
    clip: "rect(0 0 0 0)",
    height: 1,
    margin: -1,
    overflow: "hidden",
    padding: 0,
    position: "absolute",
    top: 20,
    width: 1,
  },
  selectedRow: {
    backgroundColor: "rgba(238, 114, 26, 0.08) !important",
  },
  pagination: {
    overflow: "visible",
  },
}));

export interface ITableData {
  value: any;
  searchValue: string | number;
}

interface ITableAction {
  name: string;
}
export interface ITableBodyAction extends ITableAction {
  onClick: () => boolean;
}

export interface ITableHeadAction extends ITableAction {
  onClick: (selectedRows: ITableRow[]) => boolean;
}

export interface ITableRow {
  id: string;
  route?: string;
  data: Map<string, ITableData>;
  bodyActions?: ITableBodyAction[];
  hoverText?: string;
}

interface IDataTableProps {
  headCells: ITableHeadCell[];
  headActions?: ITableHeadAction[];
  rows: ITableRow[];
  isLoading?: boolean;
  noDataMessage?: string;
  withToolbar?: boolean;
  withCheckboxes?: boolean;
  order?: "asc" | "desc";
  orderBy?: string;
  styles?: React.CSSProperties;
  count?: number;
  onPageChange?: () => void;
  subscribe?: string;
  endpointIds?: Array<string>;
  checkedEndpointIds?: Array<string>;
  checked?: (name: string, state: boolean) => void;
  hideDense?: boolean;
  roleAssignmentScript?: string;
  dataRowsPerPage?: number;
  fixedWidth?: string;
}

export default function DataTable(props: IDataTableProps) {
  const navigate = useNavigate();

  const count = props.count;
  const classes = useStyles();
  const [order, setOrder] = React.useState<"asc" | "desc">(props.order || "asc");
  const [orderBy, setOrderBy] = React.useState<string>(props.orderBy || "");
  const [selected, setSelected] = React.useState<ITableRow[]>([]);
  const [page, setPage] = React.useState(0);
  const [rowsPerPage, setRowsPerPage] = React.useState(props.dataRowsPerPage ?? 20);
  const [withCheckboxes, setWithCheckboxes] = React.useState(props.withCheckboxes || false);
  const [withToolbar, setwithToolbar] = React.useState(props.withToolbar || true);
  const [noDataMessage, setNoDataMessage] = React.useState(props.noDataMessage || "No data available");
  const [rows, setRows] = React.useState<ITableRow[]>([]);
  const [localRowsInitiated, setLocalRowsInitiated] = React.useState(false);
  const [actionsWidth, setActionsWidth] = React.useState<number>(0);
  const [hideTable, setHideTable] = React.useState(true);
  const onPageChange = props.onPageChange;
  const [headerCheckBox, setHeaderCheckBox] = React.useState(false);

  const emptyRows = rowsPerPage - Math.min(rowsPerPage, getRows.length - page * rowsPerPage);

  useEffect(() => {
    displayTableAfterMilliseconds(500);
  });

  function displayTableAfterMilliseconds(milliseconds: number): void {
    setTimeout(() => {
      setHideTable(false);
    }, milliseconds);
  }

  function getRows(): ITableRow[] {
    return localRowsInitiated ? rows : props.rows;
  }

  function sliceIntoChunks(arr: any[], chunkSize: number) {
    const res = [];
    for (let i = 0; i < arr.length; i += chunkSize) {
      const chunk = arr.slice(i, i + chunkSize);
      res.push(chunk);
    }
    return res;
  }

  const handleRequestSort = (event: any, property: any) => {
    const isAsc = orderBy === property && order === "asc";
    setOrder(isAsc ? "desc" : "asc");
    setOrderBy(property);
  };

  const handleSelectAllClick = () => {
    if (!headerCheckBox) {
      const selectedRows = sliceIntoChunks(getRows(), rowsPerPage);
      setSelected(selectedRows[page]);
      setHeaderCheckBox(true);
      return;
    }
    setSelected([]);
    setHeaderCheckBox(false);
  };

  const handleClick = (event: any, row: any) => {
    const selectedIndex = selected.indexOf(row);
    let newSelected: any[] = [];

    if (selectedIndex === -1) {
      newSelected = newSelected.concat(selected, row);
    } else if (selectedIndex === 0) {
      newSelected = newSelected.concat(selected.slice(1));
    } else if (selectedIndex === selected.length - 1) {
      newSelected = newSelected.concat(selected.slice(0, -1));
    } else if (selectedIndex > 0) {
      newSelected = newSelected.concat(selected.slice(0, selectedIndex), selected.slice(selectedIndex + 1));
    }

    setSelected(newSelected);
  };

  const handleChangePage = (event: any, newPage: any) => {
    if (onPageChange !== undefined) {
      if (getRows().length - rowsPerPage * (page + 1) <= rowsPerPage) onPageChange();
    }
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: any) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const isSelected = (row: any) => selected.indexOf(row) !== -1;

  const searchRows = (filterString: string) => {
    if (!localRowsInitiated) {
      setLocalRowsInitiated(true);
      setRows(props.rows);
    }

    const filteredRows = props.rows.filter((row) => {
      const data = Array.from(row.data.values());
      return data.some((x) => x.searchValue.toString().toLowerCase().includes(filterString));
    });
    setRows(filteredRows);
  };

  const setActionsHeaderWidth = (width: number): void => {
    if (width > actionsWidth) {
      setActionsWidth(width);
    }
  };

  let isLoading;
  if (props.isLoading) {
    isLoading = getLoadingElement();
  }

  let noData;
  if (!isLoading && getRows().length === 0) {
    noData = getNoDataElement(noDataMessage);
  }

  let toolbar;
  if (withToolbar) {
    toolbar = DataTableToolbar({
      filterFunction: searchRows,
      subscribe: props.subscribe,
      endpointIds: props.endpointIds,
      checkedEndpointIds: props.checkedEndpointIds,
      checked: props.checked,
      roleAssignmentscript: props.roleAssignmentScript!,
    });
  }

  let tableRows;
  if (!isLoading && !noData) {
    tableRows = stableSort(getRows(), getComparator(order, orderBy))
      .slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage)
      .map((row: ITableRow, index: number) => {
        const isItemSelected = isSelected(row);
        const labelId = `data-table-checkbox-${index}`;

        const tableCells = Array.from(row.data.values()).map((value, i) => {
          const first = i === 0;

          return (
            <TableCell key={row.id + i} id={first ? labelId : ""} scope={first ? "row" : ""} padding={first ? (withCheckboxes ? "none" : "normal") : undefined} variant="body">
              <Text noOfLines={1}>{value.value}</Text>
            </TableCell>
          );
        });

        let actions;
        if (row.bodyActions) {
          actions = (
            <TableCell padding="normal" variant="body">
              <Flex justifyContent="flex-end" minWidth="fit-content">
                {/* <ReactResizeDetector
                  handleWidth={true}
                  onResize={(width: number) => setActionsHeaderWidth(width)}
                /> */}
                {row.bodyActions.map((action, i) => (
                  <Button
                    key={i}
                    marginRight={row.bodyActions?.length === i + 1 ? 0 : 4}
                    onClick={(event: React.MouseEvent<any, MouseEvent>) => {
                      event.preventDefault();
                      event.stopPropagation();
                      action.onClick();
                    }}
                  >
                    {action.name}
                  </Button>
                ))}
              </Flex>
            </TableCell>
          );
        }

        const LightTooltip = withStyles((theme) => ({
          tooltip: {
            backgroundColor: theme.palette.common.white,
            color: "rgba(0, 0, 0, 0.87)",
            boxShadow: theme.shadows[1],
            fontSize: 14,
          },
        }))(Tooltip);

        return (
          <LightTooltip key={"light-tool-tip-" + row.id} arrow={true} title={row.hoverText ? row.hoverText : ""}>
            <TableRow
              component={row.route ? Link : "tr"}
              to={row.route}
              hover={true}
              role="checkbox"
              aria-checked={isItemSelected}
              tabIndex={-1}
              key={row.id}
              selected={isItemSelected}
              classes={{
                selected: classes.selectedRow,
              }}
            >
              {withCheckboxes ? (
                <TableCell padding="checkbox" variant="body">
                  <DataTableCheckbox
                    checked={isItemSelected}
                    inputProps={{ "aria-labelledby": labelId }}
                    // prevent clicking checkbox will navigate to route
                    onClick={(event: React.MouseEvent<HTMLButtonElement, MouseEvent>) => {
                      event.preventDefault();
                      event.stopPropagation();
                      handleClick(event, row);
                    }}
                  />
                </TableCell>
              ) : undefined}
              {tableCells}
              {actions}
            </TableRow>
          </LightTooltip>
        );
      });
  }

  return (
    <Paper
      className={classes.paper}
      style={{
        ...props.styles,
        ...{ visibility: hideTable ? "hidden" : "visible" },
      }}
    >
      {toolbar}
      <TableContainer>
        <Table className={classes.table} aria-labelledby="tableTitle" size={"small"} aria-label="data table" style={{ tableLayout: "fixed", width: props.fixedWidth }}>
          <DataTableHead
            classes={classes}
            numSelected={selected.length}
            order={order}
            orderBy={orderBy}
            onSelectAllClick={handleSelectAllClick}
            onRequestSort={handleRequestSort}
            rowCount={count ?? 0}
            selectedRows={selected}
            withCheckboxes={withCheckboxes}
            headCells={props.headCells}
            headerActions={props.headActions}
            headerActionsWidth={actionsWidth}
            setHeaderActionsWidth={setActionsHeaderWidth}
          />
          <TableBody>{tableRows}</TableBody>
        </Table>
      </TableContainer>
      {isLoading}
      <TablePagination
        rowsPerPageOptions={[]}
        component="div"
        count={count ?? 0} // XXX ROWCOUNT
        rowsPerPage={rowsPerPage}
        page={page}
        onPageChange={handleChangePage}
        onRowsPerPageChange={handleChangeRowsPerPage}
        classes={{
          root: classes.pagination,
        }}
      ></TablePagination>
    </Paper>
  );
}

const getLoadingElement = (): JSX.Element => {
  return (
    <Flex justifyContent="center" paddingTop="20px">
      <Loading />
    </Flex>
  );
};

const getNoDataElement = (message: string): JSX.Element => {
  return <TableRow style={{ display: "inline-block", padding: "16px 0 0 16px" }}>{message}</TableRow>;
};

function stableSort(array: any[], comparator: (a: any, b: any) => number) {
  const stabilizedThis = array.map((el, index) => [el, index]);
  stabilizedThis.sort((a, b) => {
    const order = comparator(a[0], b[0]);
    if (order !== 0) return order;
    return a[1] - b[1];
  });
  return stabilizedThis.map((el) => el[0]);
}

function getComparator(order: string, orderBy: string): (a: any, b: any) => number {
  return order === "desc" ? (a: any, b: any) => descendingComparator(a, b, orderBy) : (a: any, b: any) => -descendingComparator(a, b, orderBy);
}

function descendingComparator(a: any, b: any, orderBy: string) {
  const aSearchValue = a.data.get(orderBy)?.searchValue;
  const bSearchValue = b.data.get(orderBy)?.searchValue;

  if (bSearchValue < aSearchValue) {
    return -1;
  }
  if (bSearchValue > aSearchValue) {
    return 1;
  }
  return 0;
}

function removeDefaultFieldsFromRow(row: ITableRow): ITableData[] {
  return Object.entries(row)
    .filter((x) => !(x[0] === "id" || x[0] === "route"))
    .map((x) => x[1])[0];
}
