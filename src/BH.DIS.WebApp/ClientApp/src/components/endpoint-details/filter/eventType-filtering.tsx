import * as React from "react";
import * as api from "api-client";
import FilterContext from "./filtering-context";
import { useParams } from "react-router-dom";
import { InputLabel, Box, FormControl, MenuItem, Checkbox, OutlinedInput, ListItemText, TextField } from "@material-ui/core";
import Autocomplete from "@material-ui/lab/Autocomplete";
import CheckBoxOutlineBlankIcon from "@material-ui/icons/CheckBoxOutlineBlank";
import CheckBoxIcon from "@material-ui/icons/CheckBox";
import { createStyles, makeStyles, Theme } from "@material-ui/core/styles";

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      width: "-webkit-fill-available",
      paddingLeft: 16,
      "& > * + *": {
        marginTop: theme.spacing(2),
      },
    },
  })
);

type EventTypeFilteringProps = {};

const EventTypeFiltering = (props: EventTypeFilteringProps) => {
  const classes = useStyles();
  const client = new api.Client(api.CookieAuth());
  const params = useParams();

  const ctx = React.useContext(FilterContext);
  const [eventTypes, setEventTypes] = React.useState<string[]>([]);

  React.useEffect(() => {
    const fetchData = async () => {
      const result = await client.getEventtypesByEndpointId(params.id!);

      const consumes = result.consumes
        ?.map((event) => event.events!)
        .reduce((pre, cur) => pre.concat(cur), [])
        .map((event) => event.name!);

      const produces = result.produces
        ?.map((event) => event.events!)
        .reduce((pre, cur) => pre.concat(cur), [])
        .map((event) => event.name!);

      setEventTypes([...consumes!, ...produces!]);
    };
    fetchData();
  }, []);

  return (
    <div className={classes.root}>
      <Autocomplete
        multiple
        size="small"
        // multiple
        // limitTags={2}
        id="checkboxes-eventTypes"
        options={eventTypes}
        disableCloseOnSelect={true}
        getOptionLabel={(option) => option}
        renderOption={(option, { selected }) => (
          <React.Fragment>
            <Checkbox icon={<CheckBoxOutlineBlankIcon fontSize="small" />} checkedIcon={<CheckBoxIcon fontSize="small" />} style={{ marginRight: 8 }} checked={selected} />
            {option}
          </React.Fragment>
        )}
        onChange={(event: any, newValue: string[] | null) => {
          const filters = ctx.filterContext;
          filters.eventTypeId = newValue!;

          ctx.setProjectContext(filters);
        }}
        style={{}}
        renderInput={(parameters) => <TextField {...parameters} variant="outlined" label="EventTypes" placeholder="EventTypes" />}
      />
    </div>
  );
};

export default EventTypeFiltering;
