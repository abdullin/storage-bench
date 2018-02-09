package bench

import (
	"log"
	"os"
	"testing"

	"github.com/bmatsuo/lmdb-go/lmdb"
	"github.com/pkg/errors"

	fb "github.com/google/flatbuffers/go"
)

var (
	db  lmdb.DBI
	env *lmdb.Env
)

func init() {

	var folder = "tmp"
	os.RemoveAll(folder)

	var err error
	env, err = lmdb.NewEnv()

	if err != nil {
		panic(errors.Wrap(err, "env create failed"))
	}

	err = env.SetMaxDBs(1)
	if err != nil {
		panic(err)
	}
	err = env.SetMapSize(1 << 20)
	if err != nil {
		panic("map size failed")
	}

	if err = env.SetFlags(lmdb.MapAsync); err != nil {
		panic("set flag")
	}

	os.MkdirAll(folder, os.ModePerm)
	err = env.Open(folder, 0, os.ModePerm)
	if err != nil {
		panic(errors.Wrap(err, "open env in "+folder))
	}

	var staleReaders int
	if staleReaders, err = env.ReaderCheck(); err != nil {
		panic(errors.Wrap(err, "reader check"))
	}
	if staleReaders > 0 {
		log.Printf("cleared %d reader slots from dead processes", staleReaders)
	}

	err = env.Update(func(txn *lmdb.Txn) (err error) {
		db, err = txn.CreateDBI("agg")
		return err
	})
	if err != nil {
		panic(errors.Wrap(err, "create DB"))
	}
}

const (
	BinItemCount    int    = 5
	RestockCount           = 20
	ProductIDOffset        = 100000
	BinCode                = "RandomBinHere"
	SaleID          uint64 = 1234023
	BinFlags               = BinFlagsHasSale
	WriteTx                = lmdb.NoSync
)

func CreateBin(key []byte) {

	tx, err := env.BeginTxn(nil, 0)
	if err != nil {
		panic(err)
	}

	buf := fb.NewBuilder(1024)
	BinStartItemsVector(buf, BinItemCount)

	for i := 0; i < BinItemCount; i++ {
		itemID := uint64(i + ProductIDOffset)
		CreateBinItem(buf, itemID, 0, uint32(i+1), ItemTypeProduct)
	}
	bins := buf.EndVector(BinItemCount)
	code := buf.CreateString(BinCode)

	BinStart(buf)
	BinAddItems(buf, bins)
	BinAddType(buf, ItemTypeBin)
	BinAddFlags(buf, BinFlags)
	BinAddCode(buf, code)
	BinAddSubtype(buf, 1)
	BinAddSaleID(buf, SaleID)
	bin := BinEnd(buf)
	buf.Finish(bin)

	tx.Put(db, key, buf.FinishedBytes(), 0)
	tx.Commit()

}

func BenchmarkAdd(b *testing.B) {

	var (
		data []byte

		item = &BinItem{}
		key  = []byte{1}
	)
	CreateBin(key)

	for i := 0; i < b.N; i++ {
		tx, err := env.BeginTxn(nil, WriteTx)
		if err != nil {
			panic(err)
		}
		data, err = tx.Get(db, key)
		if err != nil {
			panic(err)
		}

		bin := GetRootAsBin(data, 0)
		search := uint64(i%BinItemCount + ProductIDOffset)
		for j := 0; j < bin.ItemsLength(); j++ {
			bin.Items(item, j)
			if item.ItemID() == search {
				item.MutateCount(item.Count() + 1)
				break
			}
		}
		tx.Put(db, key, data, 0)
		tx.Commit()
	}

}
